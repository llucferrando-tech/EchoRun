import argparse
import json
import random
from pathlib import Path

import librosa
import matplotlib.pyplot as plt
import numpy as np


LANES = [-1, 0, 1]
OBSTACLE_TYPES = {"Wall", "Gap", "Aerial"}


# =========================================================
# SIMPLE TUNING
# =========================================================

USE_HIGH_ENERGY_ONSETS = True
ONSET_STRENGTH_THRESHOLD = 0.55
HIGH_ENERGY_ONSET_THRESHOLD = 0.55

LONG_WALL_MIN_DURATION = 0.90
LONG_WALL_MAX_DURATION = 1.40
LONG_WALL_COOLDOWN = 2.20

AERIAL_COOLDOWN = 1.80

# spacing is relative to beat interval
MIN_SPACING_BEAT_FACTOR = 0.8
MIN_SPACING_FLOOR = 0.65

# base event density by energy
EVENT_CHANCE_LOW = 0.50
EVENT_CHANCE_MID = 0.72
EVENT_CHANCE_HIGH = 0.86

# base obstacle weights
# low energy
WALL_WEIGHT_LOW = 0.58
GAP_WEIGHT_LOW = 0.26
AERIAL_WEIGHT_LOW = 0.16

# mid energy
WALL_WEIGHT_MID = 0.48
GAP_WEIGHT_MID = 0.30
AERIAL_WEIGHT_MID = 0.22

# high energy
WALL_WEIGHT_HIGH = 0.38
GAP_WEIGHT_HIGH = 0.33
AERIAL_WEIGHT_HIGH = 0.29

# group chances
WALL_PAIR_CHANCE_LOW = 0.08
WALL_PAIR_CHANCE_MID = 0.15
WALL_PAIR_CHANCE_HIGH = 0.22

LONG_WALL_CHANCE_LOW = 0.05
LONG_WALL_CHANCE_MID = 0.10
LONG_WALL_CHANCE_HIGH = 0.16

FULL_GAP_CHANCE_LOW = 0.12
FULL_GAP_CHANCE_MID = 0.22
FULL_GAP_CHANCE_HIGH = 0.32

FULL_AERIAL_CHANCE_LOW = 0.06
FULL_AERIAL_CHANCE_MID = 0.14
FULL_AERIAL_CHANCE_HIGH = 0.22

# kick / pulse tuning
KICK_EVENT_BOOST = 0.22
KICK_WALL_BOOST = 0.35
STRONG_KICK_THRESHOLD = 0.60
INTRO_FLOATY_KICK_THRESHOLD = 0.22
INTRO_FLOATY_BASS_THRESHOLD = 0.28
INTRO_FLOATY_CHANCE_MULT = 0.55


# =========================================================
# BASIC UTILS
# =========================================================

def clamp_lane(value: int) -> int:
    return max(-1, min(1, int(value)))


def round_time(value: float) -> float:
    return round(float(value), 3)


def normalize_feature(x):
    x = np.asarray(x, dtype=np.float32)
    if len(x) == 0:
        return x

    mn = np.min(x)
    mx = np.max(x)
    if mx - mn < 1e-8:
        return np.zeros_like(x)

    return (x - mn) / (mx - mn)


def sample_feature_at_time(times, values, t):
    if len(times) == 0 or len(values) == 0:
        return 0.0

    idx = int(np.searchsorted(times, t))
    idx = max(0, min(idx, len(values) - 1))
    return float(values[idx])


def make_event(time_s: float, lane: int, obstacle_type: str, duration: float = 0.0):
    lane = clamp_lane(lane)

    if obstacle_type not in OBSTACLE_TYPES:
        raise ValueError(f"Invalid obstacle type: {obstacle_type}")

    if obstacle_type != "Wall":
        duration = 0.0

    return {
        "time": round_time(time_s),
        "lane": lane,
        "obstacleType": obstacle_type,
        "duration": round(max(0.0, float(duration)), 3),
    }


def estimate_scroll_speed(bpm: float) -> float:
    speed = 5.0 + (bpm / 60.0) * 1.2
    return round(max(6.0, min(12.0, speed)), 2)


def weighted_choice(items, weights, rng: random.Random):
    total = sum(weights)
    if total <= 0:
        return items[0]

    r = rng.random() * total
    acc = 0.0
    for item, w in zip(items, weights):
        acc += w
        if r <= acc:
            return item
    return items[-1]


def legal_two_lane_templates():
    return [
        [-1, 0],
        [0, 1],
        [-1, 1],
    ]


def median_beat_interval(beat_times):
    if len(beat_times) < 2:
        return 0.5
    return float(np.median(np.diff(beat_times)))


def is_strong_kick(features, t: float) -> bool:
    kick_val = sample_feature_at_time(features["kick_times"], features["kick_pulse"], t)
    return kick_val >= STRONG_KICK_THRESHOLD


# =========================================================
# MUSIC ANALYSIS
# =========================================================

def analyze_song(song_path: Path):
    y, sr = librosa.load(song_path, sr=None, mono=True)
    y, _ = librosa.effects.trim(y, top_db=25)

    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)
    tempo = float(np.squeeze(tempo))
    beat_times = librosa.frames_to_time(beat_frames, sr=sr)

    # Split harmonic/percussive so we can better detect punchy rhythmic hits
    y_harmonic, y_percussive = librosa.effects.hpss(y)

    # General onset envelope
    onset_env = librosa.onset.onset_strength(y=y, sr=sr)
    onset_env_norm = normalize_feature(onset_env)

    onset_frames = librosa.onset.onset_detect(
        onset_envelope=onset_env,
        sr=sr,
        backtrack=False,
        units="frames",
    )
    onset_times = librosa.frames_to_time(onset_frames, sr=sr)

    # Energy
    rms = librosa.feature.rms(y=y)[0]
    rms_times = librosa.times_like(rms, sr=sr)
    rms_norm = normalize_feature(rms)

    # Full-spectrum frequency features
    stft_mag = np.abs(librosa.stft(y))
    freqs = librosa.fft_frequencies(sr=sr)

    bass_mask = freqs < 200
    treble_mask = freqs > 2000

    bass_energy = (
        stft_mag[bass_mask].mean(axis=0)
        if np.any(bass_mask)
        else np.zeros(stft_mag.shape[1], dtype=np.float32)
    )
    treble_energy = (
        stft_mag[treble_mask].mean(axis=0)
        if np.any(treble_mask)
        else np.zeros(stft_mag.shape[1], dtype=np.float32)
    )

    bass_energy = normalize_feature(bass_energy)
    treble_energy = normalize_feature(treble_energy)
    spec_times = librosa.times_like(bass_energy, sr=sr)

    # Smoothed overall energy
    win = 43
    kernel = np.ones(win, dtype=np.float32) / win
    energy_smooth = np.convolve(rms_norm, kernel, mode="same")
    energy_smooth = normalize_feature(energy_smooth)

    # -------------------------------------------------
    # Kick / low-percussive pulse detection
    # Better for "doof doof doof" than generic bass average
    # -------------------------------------------------
    S_perc = np.abs(librosa.stft(y_percussive))
    freqs_perc = librosa.fft_frequencies(sr=sr)

    kick_mask = (freqs_perc >= 40) & (freqs_perc <= 180)
    kick_band = (
        S_perc[kick_mask].mean(axis=0)
        if np.any(kick_mask)
        else np.zeros(S_perc.shape[1], dtype=np.float32)
    )
    kick_band = normalize_feature(kick_band)
    kick_times = librosa.times_like(kick_band, sr=sr)

    kick_onset_env = librosa.onset.onset_strength(
        y=y_percussive,
        sr=sr,
        aggregate=np.mean,
    )
    kick_onset_env = normalize_feature(kick_onset_env)

    # Align lengths safely
    min_len = min(len(kick_band), len(kick_onset_env))
    if min_len == 0:
        kick_pulse = np.zeros(0, dtype=np.float32)
        kick_times = np.zeros(0, dtype=np.float32)
    else:
        kick_pulse = 0.55 * kick_band[:min_len] + 0.45 * kick_onset_env[:min_len]
        kick_pulse = normalize_feature(kick_pulse)
        kick_times = kick_times[:min_len]

    onset_times_with_strength = []
    for frame, t in zip(onset_frames, onset_times):
        strength = float(onset_env_norm[frame]) if 0 <= frame < len(onset_env_norm) else 0.0
        onset_times_with_strength.append((float(t), strength))

    return {
        "tempo": tempo,
        "beat_times": beat_times,
        "onset_times_with_strength": onset_times_with_strength,
        "rms_times": rms_times,
        "energy_smooth": energy_smooth,
        "spec_times": spec_times,
        "bass_energy": bass_energy,
        "treble_energy": treble_energy,
        "kick_times": kick_times,
        "kick_pulse": kick_pulse,
    }


# =========================================================
# CANDIDATES
# =========================================================

def build_candidate_times(features):
    beat_times = [float(t) for t in features["beat_times"]]
    candidates = [{"time": t, "kind": "beat"} for t in beat_times]

    if USE_HIGH_ENERGY_ONSETS:
        for t, strength in features["onset_times_with_strength"]:
            if strength < ONSET_STRENGTH_THRESHOLD:
                continue

            energy = sample_feature_at_time(
                features["rms_times"],
                features["energy_smooth"],
                t,
            )
            if energy >= HIGH_ENERGY_ONSET_THRESHOLD:
                candidates.append({
                    "time": float(t),
                    "kind": "onset",
                    "strength": float(strength),
                })

    # If beat tracking stops early, extend the beat grid to the end of the song.
    song_end = float(features["rms_times"][-1]) if len(features["rms_times"]) else 0.0

    if len(beat_times) >= 2:
        beat_interval = float(np.median(np.diff(beat_times)))
        last_beat = float(beat_times[-1])

        while last_beat + beat_interval < song_end - 0.2:
            last_beat += beat_interval
            candidates.append({
                "time": float(last_beat),
                "kind": "beat_extrapolated",
            })

    candidates.sort(key=lambda x: x["time"])

    filtered = []
    for c in candidates:
        if not filtered:
            filtered.append(c)
            continue

        dt = abs(c["time"] - filtered[-1]["time"])

        # keep beat grid clean but still allow meaningful extra onsets
        if c["kind"] == "onset" and filtered[-1]["kind"] == "beat" and dt < 0.12:
            continue
        if dt < 0.06:
            continue

        filtered.append(c)

    return filtered


# =========================================================
# OCCUPANCY
# =========================================================

class LaneOccupancy:
    def __init__(self):
        self.intervals = {lane: [] for lane in LANES}

    def is_occupied_at(self, lane: int, t: float) -> bool:
        for start, end in self.intervals[lane]:
            if start <= t < end:
                return True
        return False

    def is_free_for_interval(self, lane: int, start: float, end: float) -> bool:
        for a, b in self.intervals[lane]:
            if start < b and end > a:
                return False
        return True

    def reserve(self, lane: int, start: float, end: float):
        if end <= start:
            return
        self.intervals[lane].append((start, end))
        self.intervals[lane].sort(key=lambda x: x[0])

    def blocked_lanes_at(self, t: float):
        return {lane for lane in LANES if self.is_occupied_at(lane, t)}


# =========================================================
# STATE
# =========================================================

class GeneratorState:
    def __init__(self):
        self.last_event_time = -999.0
        self.last_obstacle_type = None
        self.last_aerial_time = -999.0
        self.last_wall_lanes = None
        self.last_long_wall_time = -999.0


# =========================================================
# SIMPLE RULES
# =========================================================

def energy_band(energy_val: float) -> str:
    if energy_val < 0.35:
        return "low"
    if energy_val < 0.68:
        return "mid"
    return "high"


def event_chance_for_band(band: str) -> float:
    if band == "low":
        return EVENT_CHANCE_LOW
    if band == "mid":
        return EVENT_CHANCE_MID
    return EVENT_CHANCE_HIGH


def choose_obstacle_type(
    band: str,
    bass_val: float,
    treble_val: float,
    kick_val: float,
    state: GeneratorState,
    t: float,
    rng: random.Random,
):
    if band == "low":
        wall_w, gap_w, aerial_w = WALL_WEIGHT_LOW, GAP_WEIGHT_LOW, AERIAL_WEIGHT_LOW
    elif band == "mid":
        wall_w, gap_w, aerial_w = WALL_WEIGHT_MID, GAP_WEIGHT_MID, AERIAL_WEIGHT_MID
    else:
        wall_w, gap_w, aerial_w = WALL_WEIGHT_HIGH, GAP_WEIGHT_HIGH, AERIAL_WEIGHT_HIGH

    # Bass = structure / movement
    wall_w += bass_val * 0.25

    # Kick = punch / obvious rhythmic hit
    wall_w += kick_val * KICK_WALL_BOOST

    # Treble = activity / reaction
    aerial_w += treble_val * 0.35
    gap_w += treble_val * 0.22

    # Low bass should not kill gameplay, reactions take over
    if bass_val < 0.35:
        gap_w += 0.30
        aerial_w += 0.30
        wall_w *= 0.45

    if bass_val < 0.25 and treble_val > 0.50:
        gap_w += 0.20
        aerial_w += 0.20
        wall_w *= 0.35

    # No aerial spam
    if state.last_obstacle_type == "Aerial" or (t - state.last_aerial_time) < AERIAL_COOLDOWN:
        aerial_w = 0.0

    return weighted_choice(
        ["Wall", "Gap", "Aerial"],
        [wall_w, gap_w, aerial_w],
        rng,
    )


def choose_wall_group(
    t: float,
    band: str,
    bass_val: float,
    occupancy: LaneOccupancy,
    state: GeneratorState,
    rng: random.Random,
):
    free_lanes = [lane for lane in LANES if not occupancy.is_occupied_at(lane, t)]
    if not free_lanes:
        return []

    if band == "low":
        pair_chance = WALL_PAIR_CHANCE_LOW
        long_chance = LONG_WALL_CHANCE_LOW
    elif band == "mid":
        pair_chance = WALL_PAIR_CHANCE_MID
        long_chance = LONG_WALL_CHANCE_MID
    else:
        pair_chance = WALL_PAIR_CHANCE_HIGH
        long_chance = LONG_WALL_CHANCE_HIGH

    pair_chance += bass_val * 0.10
    long_chance += bass_val * 0.08

    if len(free_lanes) >= 2 and rng.random() < pair_chance:
        legal_pairs = [tpl for tpl in legal_two_lane_templates() if set(tpl).issubset(set(free_lanes))]
        if legal_pairs:
            if state.last_wall_lanes is not None:
                preferred = [tpl for tpl in legal_pairs if tuple(sorted(tpl)) != tuple(sorted(state.last_wall_lanes))]
                if preferred:
                    legal_pairs = preferred

            pair = rng.choice(legal_pairs)
            return [make_event(t, lane, "Wall", 0.0) for lane in pair]

    candidates = list(free_lanes)
    if state.last_wall_lanes is not None and len(state.last_wall_lanes) == 1:
        preferred = [lane for lane in candidates if lane not in state.last_wall_lanes]
        if preferred:
            candidates = preferred

    lane = rng.choice(candidates)

    duration = 0.0
    long_wall_allowed = (t - state.last_long_wall_time) > LONG_WALL_COOLDOWN
    if long_wall_allowed:
        if occupancy.is_free_for_interval(lane, t, t + LONG_WALL_MAX_DURATION) and rng.random() < long_chance:
            duration = rng.uniform(LONG_WALL_MIN_DURATION, LONG_WALL_MAX_DURATION)

    return [make_event(t, lane, "Wall", duration)]


def choose_reaction_group(
    t: float,
    obstacle_type: str,
    band: str,
    occupancy: LaneOccupancy,
    rng: random.Random,
):
    free_lanes = [lane for lane in LANES if not occupancy.is_occupied_at(lane, t)]
    if len(free_lanes) < 2:
        return []

    if obstacle_type == "Gap":
        if band == "low":
            full_chance = FULL_GAP_CHANCE_LOW
        elif band == "mid":
            full_chance = FULL_GAP_CHANCE_MID
        else:
            full_chance = FULL_GAP_CHANCE_HIGH
    else:
        if band == "low":
            full_chance = FULL_AERIAL_CHANCE_LOW
        elif band == "mid":
            full_chance = FULL_AERIAL_CHANCE_MID
        else:
            full_chance = FULL_AERIAL_CHANCE_HIGH

    # full 3-lane reaction moment
    if len(free_lanes) == 3 and rng.random() < full_chance:
        return [make_event(t, lane, obstacle_type, 0.0) for lane in LANES]

    legal_pairs = [tpl for tpl in legal_two_lane_templates() if set(tpl).issubset(set(free_lanes))]
    if not legal_pairs:
        return []

    pair = rng.choice(legal_pairs)
    remaining = [lane for lane in free_lanes if lane not in pair]

    group = [make_event(t, lane, obstacle_type, 0.0) for lane in pair]

    # if reaction doesn't cover all 3 lanes, leftover lane becomes a wall
    if len(remaining) == 1:
        group.append(make_event(t, remaining[0], "Wall", 0.0))

    return group


def validate_group(group_events, occupancy: LaneOccupancy):
    if not group_events:
        return False

    t = float(group_events[0]["time"])
    lanes = [e["lane"] for e in group_events]
    if len(lanes) != len(set(lanes)):
        return False

    wall_lanes = set()
    occupied_now = occupancy.blocked_lanes_at(t)

    for event in group_events:
        if float(event["time"]) != t:
            return False

        lane = int(event["lane"])
        obstacle_type = event["obstacleType"]
        duration = float(event["duration"])

        if obstacle_type == "Wall":
            wall_lanes.add(lane)
            if duration > 0.0:
                if not occupancy.is_free_for_interval(lane, t, t + duration):
                    return False
            else:
                if occupancy.is_occupied_at(lane, t):
                    return False
        else:
            if duration != 0.0:
                return False
            if occupancy.is_occupied_at(lane, t):
                return False

    # never hard block all 3 lanes with walls/occupancy
    if len(occupied_now | wall_lanes) >= 3:
        return False

    return True


def commit_group(group_events, occupancy: LaneOccupancy, state: GeneratorState):
    if not group_events:
        return

    t = float(group_events[0]["time"])
    obstacle_type = group_events[0]["obstacleType"]

    for event in group_events:
        if event["obstacleType"] == "Wall" and float(event["duration"]) > 0.0:
            occupancy.reserve(
                event["lane"],
                float(event["time"]),
                float(event["time"]) + float(event["duration"]),
            )
            state.last_long_wall_time = float(event["time"])

    state.last_event_time = t
    state.last_obstacle_type = obstacle_type

    if any(e["obstacleType"] == "Aerial" for e in group_events):
        state.last_aerial_time = t

    wall_events = [e for e in group_events if e["obstacleType"] == "Wall"]
    if wall_events:
        state.last_wall_lanes = tuple(sorted(e["lane"] for e in wall_events))


# =========================================================
# GENERATION
# =========================================================

def generate_obstacle_events(features, rng: random.Random):
    candidates = build_candidate_times(features)
    occupancy = LaneOccupancy()
    state = GeneratorState()
    events = []

    beat_interval = median_beat_interval(features["beat_times"])
    min_spacing = max(MIN_SPACING_FLOOR, beat_interval * MIN_SPACING_BEAT_FACTOR)

    for candidate in candidates:
        t = float(candidate["time"])

        if t - state.last_event_time < min_spacing:
            continue

        energy_val = sample_feature_at_time(features["rms_times"], features["energy_smooth"], t)
        bass_val = sample_feature_at_time(features["spec_times"], features["bass_energy"], t)
        treble_val = sample_feature_at_time(features["spec_times"], features["treble_energy"], t)
        kick_val = sample_feature_at_time(features["kick_times"], features["kick_pulse"], t)

        band = energy_band(energy_val)
        chance = event_chance_for_band(band)

        # treble can keep gameplay alive too
        chance += treble_val * 0.12

        # kick should strongly support gameplay moments
        chance += kick_val * KICK_EVENT_BOOST

        if candidate["kind"] == "beat":
            chance += 0.18
            chance = max(chance, 0.78)
        elif candidate["kind"] == "onset":
            chance += 0.05
        elif candidate["kind"] == "beat_extrapolated":
            chance -= 0.03

        # clear kick pulse should strongly encourage spawning
        if kick_val > STRONG_KICK_THRESHOLD:
            chance = max(chance, 0.88)

        # if bass drops but treble stays alive, keep gameplay going
        if bass_val < 0.30 and treble_val > 0.40:
            chance = max(chance, 0.68)

        # floaty intro protection:
        # if there is not much kick and not much bass, reduce spawning
        if kick_val < INTRO_FLOATY_KICK_THRESHOLD and bass_val < INTRO_FLOATY_BASS_THRESHOLD:
            chance *= INTRO_FLOATY_CHANCE_MULT

        chance = min(0.97, max(0.05, chance))

        if rng.random() > chance:
            continue

        obstacle_type = choose_obstacle_type(
            band=band,
            bass_val=bass_val,
            treble_val=treble_val,
            kick_val=kick_val,
            state=state,
            t=t,
            rng=rng,
        )

        if obstacle_type == "Wall":
            group = choose_wall_group(t, band, bass_val, occupancy, state, rng)
        else:
            group = choose_reaction_group(t, obstacle_type, band, occupancy, rng)

        if not validate_group(group, occupancy):
            continue

        commit_group(group, occupancy, state)
        events.extend(group)

    events.sort(key=lambda e: (e["time"], e["lane"], e["obstacleType"]))
    return events


# =========================================================
# OUTPUT / GRAPH
# =========================================================

def build_level_data(song_path: Path, scroll_speed=None, seed=42):
    rng = random.Random(seed)
    features = analyze_song(song_path)
    events = generate_obstacle_events(features, rng)

    bpm = features["tempo"]
    if scroll_speed is None:
        scroll_speed = estimate_scroll_speed(bpm)

    level_data = {
        "songName": song_path.stem,
        "bpm": round(float(bpm), 3),
        "scrollSpeed": float(scroll_speed),
        "difficultyMode": "simple",
        "events": events,
    }

    print(f"Detected beats: {len(features['beat_times'])}")
    if len(features["beat_times"]) > 0:
        print(f"First beat: {float(features['beat_times'][0]):.3f}")
        print(f"Last detected beat: {float(features['beat_times'][-1]):.3f}")
        print(f"Median beat interval: {median_beat_interval(features['beat_times']):.3f}")
    if len(features["rms_times"]) > 0:
        print(f"Song end from RMS: {float(features['rms_times'][-1]):.3f}")

    return level_data, features


def export_level_json(level_data: dict, output_dir: Path):
    output_dir.mkdir(parents=True, exist_ok=True)
    output_path = output_dir / f"level_{level_data['songName']}.json"

    with output_path.open("w", encoding="utf-8") as f:
        json.dump(level_data, f, indent=2)

    return output_path


def save_debug_graph(song_name: str, features: dict, level_data: dict, output_dir: Path):
    output_dir.mkdir(parents=True, exist_ok=True)
    out_path = output_dir / f"debug_{song_name}.png"

    rms_times = features["rms_times"]
    energy_smooth = features["energy_smooth"]
    spec_times = features["spec_times"]
    bass_energy = features["bass_energy"]
    treble_energy = features["treble_energy"]
    kick_times = features["kick_times"]
    kick_pulse = features["kick_pulse"]
    beat_times = features["beat_times"]
    events = level_data["events"]

    fig = plt.figure(figsize=(15, 8))

    ax1 = plt.subplot(4, 1, 1)
    ax1.plot(rms_times, energy_smooth, linewidth=2)
    ax1.set_title(f"{song_name} - Beat-Driven Debug View")
    ax1.set_ylabel("Energy")
    ax1.set_ylim(0, 1.05)
    ax1.grid(True, alpha=0.25)

    for bt in beat_times:
        ax1.axvline(float(bt), alpha=0.08)

    ax2 = plt.subplot(4, 1, 2, sharex=ax1)
    ax2.plot(spec_times, bass_energy, linewidth=2, label="Bass")
    ax2.plot(spec_times, treble_energy, linewidth=2, label="Treble")
    ax2.set_ylabel("Freq energy")
    ax2.set_ylim(0, 1.05)
    ax2.legend()
    ax2.grid(True, alpha=0.25)

    ax3 = plt.subplot(4, 1, 3, sharex=ax1)
    if len(kick_times) and len(kick_pulse):
        ax3.plot(kick_times, kick_pulse, linewidth=2, label="Kick Pulse")
    ax3.set_ylabel("Kick")
    ax3.set_ylim(0, 1.05)
    ax3.legend()
    ax3.grid(True, alpha=0.25)

    ax4 = plt.subplot(4, 1, 4, sharex=ax1)

    lane_y = {-1: 0, 0: 1, 1: 2}
    type_marker = {"Wall": "o", "Gap": "s", "Aerial": "^"}

    for obstacle_type in ["Wall", "Gap", "Aerial"]:
        xs = [e["time"] for e in events if e["obstacleType"] == obstacle_type]
        ys = [lane_y[e["lane"]] for e in events if e["obstacleType"] == obstacle_type]
        if xs:
            ax4.scatter(xs, ys, marker=type_marker[obstacle_type], s=60, label=obstacle_type)

    for e in events:
        if e["duration"] > 0:
            y = lane_y[e["lane"]]
            ax4.hlines(y, e["time"], e["time"] + e["duration"], linewidth=3)

    ax4.set_yticks([0, 1, 2])
    ax4.set_yticklabels(["Lane -1", "Lane 0", "Lane 1"])
    ax4.set_xlabel("Time (seconds)")
    ax4.set_ylabel("Obstacles")
    ax4.legend()
    ax4.grid(True, alpha=0.25)

    plt.tight_layout()
    plt.savefig(out_path, dpi=160)
    plt.close(fig)

    return out_path


# =========================================================
# CLI
# =========================================================

def main():
    parser = argparse.ArgumentParser(
        description="Simple beat-driven procedural obstacle generator using librosa."
    )
    parser.add_argument("song", type=str, help="Path to input .wav or .mp3")
    parser.add_argument("--output-dir", type=str, default="output", help="Output folder")
    parser.add_argument("--scroll-speed", type=float, default=None, help="Override scroll speed")
    parser.add_argument("--seed", type=int, default=42, help="Random seed")

    args = parser.parse_args()

    song_path = Path(args.song)
    if not song_path.exists():
        raise FileNotFoundError(f"Song file not found: {song_path}")

    if song_path.suffix.lower() not in {".wav", ".mp3"}:
        raise ValueError("Input file must be a .wav or .mp3")

    output_dir = Path(args.output_dir)

    level_data, features = build_level_data(
        song_path=song_path,
        scroll_speed=args.scroll_speed,
        seed=args.seed,
    )

    json_path = export_level_json(level_data, output_dir)
    graph_path = save_debug_graph(level_data["songName"], features, level_data, output_dir)

    print("Done.")
    print(f"Song: {level_data['songName']}")
    print(f"BPM: {level_data['bpm']}")
    print(f"Scroll Speed: {level_data['scrollSpeed']}")
    print(f"Difficulty Mode: {level_data['difficultyMode']}")
    print(f"Events: {len(level_data['events'])}")
    print(f"JSON: {json_path}")
    print(f"Graph: {graph_path}")


if __name__ == "__main__":
    main()