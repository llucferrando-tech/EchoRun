import argparse
import json
import math
import random
from pathlib import Path

import librosa
import matplotlib.pyplot as plt
import numpy as np


LANES = [-1, 0, 1]
OBSTACLE_TYPES = {"Wall", "Gap", "Aerial"}

# =========================================================
# TUNING
# =========================================================

# Minimum spacing BETWEEN TIME GROUPS, not between individual obstacles.
# Obstacles inside the same group can share the exact same timestamp.
MIN_GROUP_SPACING_EASY = 0.75
MIN_GROUP_SPACING_MEDIUM = 0.55
MIN_GROUP_SPACING_HARD = 0.40

# Simultaneous obstacle generation
ALLOW_SIMULTANEOUS_OBSTACLES = True
MAX_SIMULTANEOUS_OBSTACLES = 2   # set to 3 if you want full lane stacks possible
SIMULTANEOUS_CHANCE_MEDIUM = 0.25
SIMULTANEOUS_CHANCE_HARD = 0.45

# Duration tuning
LONG_WALL_CHANCE = 0.22
LONG_WALL_MIN_DURATION = 0.8
LONG_WALL_MAX_DURATION = 1.8

LONG_GAP_CHANCE = 0.25
LONG_GAP_MIN_DURATION = 0.6
LONG_GAP_MAX_DURATION = 1.2


def clamp_lane(value: int) -> int:
    return max(-1, min(1, int(value)))


def round_time(value: float) -> float:
    return round(float(value), 3)


def make_event(time_s: float, lane: int, obstacle_type: str, duration: float = 0.0):
    if obstacle_type not in OBSTACLE_TYPES:
        raise ValueError(f"Invalid obstacle type: {obstacle_type}")

    return {
        "time": round_time(time_s),
        "lane": clamp_lane(lane),
        "obstacleType": obstacle_type,
        "duration": round(max(0.0, float(duration)), 3),
    }


def estimate_scroll_speed(bpm: float) -> float:
    speed = 5.0 + (bpm / 60.0) * 1.2
    return round(max(6.0, min(12.0, speed)), 2)


def valid_next_lane(candidates, history):
    if len(history) < 2:
        return list(candidates)

    if history[-1] == history[-2]:
        blocked = history[-1]
        filtered = [c for c in candidates if c != blocked]
        if filtered:
            return filtered

    return list(candidates)


def normalize_feature(x):
    x = np.asarray(x, dtype=np.float32)
    if len(x) == 0:
        return x

    mn = np.min(x)
    mx = np.max(x)

    if mx - mn < 1e-8:
        return np.zeros_like(x)

    return (x - mn) / (mx - mn)


def analyze_song(song_path: Path):
    y, sr = librosa.load(song_path, sr=None, mono=True)
    y, _ = librosa.effects.trim(y, top_db=25)

    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)
    tempo = float(np.squeeze(tempo))
    beat_times = librosa.frames_to_time(beat_frames, sr=sr)

    onset_env = librosa.onset.onset_strength(y=y, sr=sr)
    onset_frames = librosa.onset.onset_detect(
        onset_envelope=onset_env,
        sr=sr,
        backtrack=False,
        units="frames"
    )
    onset_times = librosa.frames_to_time(onset_frames, sr=sr)

    rms = librosa.feature.rms(y=y)[0]
    rms_times = librosa.times_like(rms, sr=sr)
    rms_norm = normalize_feature(rms)

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

    win = 43
    kernel = np.ones(win, dtype=np.float32) / win
    energy_smooth = np.convolve(rms_norm, kernel, mode="same")
    energy_smooth = normalize_feature(energy_smooth)

    energy_diff = np.diff(energy_smooth, prepend=energy_smooth[0])
    energy_diff = normalize_feature(energy_diff)

    return {
        "y": y,
        "sr": sr,
        "tempo": tempo,
        "beat_times": beat_times,
        "onset_times": onset_times,
        "rms": rms_norm,
        "rms_times": rms_times,
        "bass_energy": bass_energy,
        "treble_energy": treble_energy,
        "spec_times": spec_times,
        "energy_smooth": energy_smooth,
        "energy_diff": energy_diff,
    }


def sample_feature_at_time(times, values, t):
    if len(times) == 0 or len(values) == 0:
        return 0.0

    idx = int(np.searchsorted(times, t))
    idx = max(0, min(idx, len(values) - 1))
    return float(values[idx])


def classify_section_difficulty(energy_value):
    if energy_value < 0.30:
        return "easy"
    if energy_value < 0.65:
        return "medium"
    return "hard"


def group_spacing_for_difficulty(difficulty: str) -> float:
    if difficulty == "easy":
        return MIN_GROUP_SPACING_EASY
    if difficulty == "medium":
        return MIN_GROUP_SPACING_MEDIUM
    return MIN_GROUP_SPACING_HARD


def choose_obstacle_type(bass_value, treble_value, energy_value, drop_value, rng):
    if drop_value > 0.82 and energy_value > 0.55:
        return rng.choice(["Wall", "Aerial", "Wall"])

    if bass_value > treble_value + 0.12:
        if energy_value > 0.70:
            return rng.choice(["Wall", "Wall", "Gap"])
        return rng.choice(["Wall", "Gap", "Wall"])

    if treble_value > bass_value + 0.12:
        if energy_value > 0.60:
            return rng.choice(["Aerial", "Aerial", "Wall"])
        return rng.choice(["Aerial", "Wall"])

    if energy_value < 0.28:
        return rng.choice(["Gap", "Wall"])
    if energy_value > 0.75:
        return rng.choice(["Wall", "Aerial", "Wall"])

    return rng.choice(["Wall", "Gap", "Aerial"])


def choose_duration(obstacle_type, energy_value, rng):
    if obstacle_type == "Wall" and energy_value > 0.72 and rng.random() < LONG_WALL_CHANCE:
        return rng.uniform(LONG_WALL_MIN_DURATION, LONG_WALL_MAX_DURATION)

    if obstacle_type == "Gap" and energy_value < 0.35 and rng.random() < LONG_GAP_CHANCE:
        return rng.uniform(LONG_GAP_MIN_DURATION, LONG_GAP_MAX_DURATION)

    return 0.0


def choose_lane(lane_history, used_lanes_this_group, preferred=None, rng=None):
    candidates = [lane for lane in LANES if lane not in used_lanes_this_group]
    candidates = valid_next_lane(candidates, lane_history)

    if not candidates:
        candidates = [lane for lane in LANES if lane not in used_lanes_this_group]

    if not candidates:
        return None

    if preferred in candidates:
        return preferred

    return rng.choice(candidates)


def choose_group_size(difficulty: str, energy_value: float, drop_value: float, rng: random.Random) -> int:
    if not ALLOW_SIMULTANEOUS_OBSTACLES:
        return 1

    if difficulty == "easy":
        return 1

    if difficulty == "medium":
        if rng.random() < SIMULTANEOUS_CHANCE_MEDIUM:
            return min(MAX_SIMULTANEOUS_OBSTACLES, 2)
        return 1

    # hard
    if drop_value > 0.80 or energy_value > 0.80:
        if MAX_SIMULTANEOUS_OBSTACLES >= 3 and rng.random() < 0.20:
            return 3

    if rng.random() < SIMULTANEOUS_CHANCE_HARD:
        return min(MAX_SIMULTANEOUS_OBSTACLES, 2)

    return 1


def generate_group_events(t, difficulty, bass_val, treble_val, energy_val, drop_val, lane_history, rng):
    group_size = choose_group_size(difficulty, energy_val, drop_val, rng)
    group_size = max(1, min(group_size, len(LANES)))

    used_lanes = set()
    group_events = []

    primary_type = choose_obstacle_type(bass_val, treble_val, energy_val, drop_val, rng)

    for i in range(group_size):
        obstacle_type = primary_type

        # For multi-obstacle groups, bias towards matching types for cleaner patterns,
        # but allow some variation.
        if i > 0 and rng.random() < 0.30:
            obstacle_type = choose_obstacle_type(bass_val, treble_val, energy_val, drop_val, rng)

        preferred_lane = None
        if obstacle_type == "Aerial" and treble_val > 0.65:
            side_candidates = [lane for lane in (-1, 1) if lane not in used_lanes]
            if side_candidates:
                preferred_lane = rng.choice(side_candidates)
        elif obstacle_type == "Wall" and bass_val > 0.65 and 0 not in used_lanes:
            preferred_lane = 0

        lane = choose_lane(
            lane_history=lane_history,
            used_lanes_this_group=used_lanes,
            preferred=preferred_lane,
            rng=rng
        )

        if lane is None:
            continue

        duration = choose_duration(obstacle_type, energy_val, rng)

        # Keep simultaneous groups cleaner:
        # avoid multiple long durations at the exact same timestamp most of the time.
        if i > 0 and duration > 0 and rng.random() < 0.75:
            duration = 0.0

        group_events.append(make_event(t, lane, obstacle_type, duration))
        used_lanes.add(lane)

    return group_events


def generate_obstacle_events(features, rng: random.Random):
    beat_times = features["beat_times"]
    onset_times = features["onset_times"]

    timeline = sorted(set(np.round(np.concatenate([beat_times, onset_times]), 3)))

    if len(timeline) == 0:
        return []

    events = []
    lane_history = []
    last_group_time = -math.inf

    for t in timeline:
        bass_val = sample_feature_at_time(features["spec_times"], features["bass_energy"], t)
        treble_val = sample_feature_at_time(features["spec_times"], features["treble_energy"], t)
        energy_val = sample_feature_at_time(features["rms_times"], features["energy_smooth"], t)
        drop_val = sample_feature_at_time(features["rms_times"], features["energy_diff"], t)

        difficulty = classify_section_difficulty(energy_val)
        min_group_spacing = group_spacing_for_difficulty(difficulty)

        if t - last_group_time < min_group_spacing:
            continue

        group_events = generate_group_events(
            t=t,
            difficulty=difficulty,
            bass_val=bass_val,
            treble_val=treble_val,
            energy_val=energy_val,
            drop_val=drop_val,
            lane_history=lane_history,
            rng=rng
        )

        if not group_events:
            continue

        events.extend(group_events)

        for event in group_events:
            lane_history.append(event["lane"])

        last_group_time = t

    events.sort(key=lambda e: (e["time"], e["lane"]))
    return events


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
        "events": events,
    }

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
    events = level_data["events"]

    fig = plt.figure(figsize=(15, 7))

    ax1 = plt.subplot(3, 1, 1)
    ax1.plot(rms_times, energy_smooth, linewidth=2)
    ax1.set_title(f"{song_name} - Simplified Debug View")
    ax1.set_ylabel("Energy")
    ax1.set_ylim(0, 1.05)
    ax1.grid(True, alpha=0.25)

    ax2 = plt.subplot(3, 1, 2, sharex=ax1)
    ax2.plot(spec_times, bass_energy, linewidth=2, label="Bass")
    ax2.plot(spec_times, treble_energy, linewidth=2, label="Treble")
    ax2.set_ylabel("Freq energy")
    ax2.set_ylim(0, 1.05)
    ax2.legend()
    ax2.grid(True, alpha=0.25)

    ax3 = plt.subplot(3, 1, 3, sharex=ax1)

    lane_y = {-1: 0, 0: 1, 1: 2}
    type_marker = {"Wall": "o", "Gap": "s", "Aerial": "^"}

    for obstacle_type in ["Wall", "Gap", "Aerial"]:
        xs = [e["time"] for e in events if e["obstacleType"] == obstacle_type]
        ys = [lane_y[e["lane"]] for e in events if e["obstacleType"] == obstacle_type]
        if xs:
            ax3.scatter(xs, ys, marker=type_marker[obstacle_type], s=60, label=obstacle_type)

    for e in events:
        if e["duration"] > 0:
            y = lane_y[e["lane"]]
            ax3.hlines(y, e["time"], e["time"] + e["duration"], linewidth=3)

    ax3.set_yticks([0, 1, 2])
    ax3.set_yticklabels(["Lane -1", "Lane 0", "Lane 1"])
    ax3.set_xlabel("Time (seconds)")
    ax3.set_ylabel("Obstacles")
    ax3.legend()
    ax3.grid(True, alpha=0.25)

    plt.tight_layout()
    plt.savefig(out_path, dpi=160)
    plt.close(fig)

    return out_path


def main():
    parser = argparse.ArgumentParser(
        description="Analyze a song and generate obstacle JSON + simplified debug graph."
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
    print(f"Events: {len(level_data['events'])}")
    print(f"JSON: {json_path}")
    print(f"Graph: {graph_path}")


if __name__ == "__main__":
    main()