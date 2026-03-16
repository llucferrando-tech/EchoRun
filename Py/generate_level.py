import argparse
import json
import math
import random
from pathlib import Path

import librosa
import numpy as np


LANES = [-1, 0, 1]
OBSTACLE_TYPES = {"Wall", "Gap", "Aerial"}


def clamp_lane(value: int) -> int:
    if value < -1:
        return -1
    if value > 1:
        return 1
    return value


def round_time(value: float) -> float:
    return round(float(value), 3)


def choose_stride(beat_times: np.ndarray) -> int:
    """
    Use every 2nd beat or every 4th beat depending on density.
    Faster songs / denser beats -> use every 4th beat more often.
    """
    if len(beat_times) < 8:
        return 2

    intervals = np.diff(beat_times)
    median_interval = float(np.median(intervals)) if len(intervals) > 0 else 0.5

    # If beats are very dense, use every 4th beat.
    # 0.38s ~ 158 BPM quarter-note spacing.
    if median_interval < 0.38:
        return 4

    return 2


def estimate_scroll_speed(bpm: float) -> float:
    """
    Simple heuristic to produce a usable scroll speed from BPM.
    Clamped to a sensible endless-runner range.
    """
    speed = 5.0 + (bpm / 60.0) * 1.2
    return round(max(6.0, min(12.0, speed)), 2)


def valid_next_lane(candidates, history):
    """
    Prevent repeating the same lane more than 2 times in a row.
    """
    if len(history) < 2:
        return candidates[:]

    if history[-1] == history[-2]:
        blocked = history[-1]
        filtered = [c for c in candidates if c != blocked]
        if filtered:
            return filtered

    return candidates[:]


def make_event(time_s: float, lane: int, obstacle_type: str, duration: float = 0.0):
    lane = clamp_lane(lane)
    if obstacle_type not in OBSTACLE_TYPES:
        raise ValueError(f"Invalid obstacle type: {obstacle_type}")

    return {
        "time": round_time(time_s),
        "lane": lane,
        "obstacleType": obstacle_type,
        "duration": round(float(max(0.0, duration)), 3),
    }


def phrase_center_left(base_times, lane_history):
    pattern = [0, -1]
    pattern = apply_lane_history_constraints(pattern, lane_history)
    return [
        make_event(base_times[0], pattern[0], "Wall", 0.0),
        make_event(base_times[1], pattern[1], "Wall", 0.0),
    ]


def phrase_left_center_right(base_times, lane_history):
    pattern = [-1, 0, 1]
    pattern = apply_lane_history_constraints(pattern, lane_history)
    return [
        make_event(base_times[0], pattern[0], "Wall", 0.0),
        make_event(base_times[1], pattern[1], "Wall", 0.0),
        make_event(base_times[2], pattern[2], "Wall", 0.0),
    ]


def phrase_right_center_left(base_times, lane_history):
    pattern = [1, 0, -1]
    pattern = apply_lane_history_constraints(pattern, lane_history)
    return [
        make_event(base_times[0], pattern[0], "Wall", 0.0),
        make_event(base_times[1], pattern[1], "Wall", 0.0),
        make_event(base_times[2], pattern[2], "Wall", 0.0),
    ]


def phrase_long_wall_then_gap(base_times, lane_history, rng):
    long_lane = rng.choice(valid_next_lane(LANES, lane_history))
    other_lanes = [l for l in LANES if l != long_lane]
    gap_lane = rng.choice(other_lanes)

    duration = rng.uniform(1.0, 2.5)
    return [
        make_event(base_times[0], long_lane, "Wall", duration),
        make_event(base_times[1], gap_lane, "Gap", rng.uniform(0.7, 1.4)),
    ]


def phrase_aerial_mix(base_times, lane_history, rng):
    first_lane = rng.choice(valid_next_lane(LANES, lane_history))
    second_candidates = [l for l in LANES if l != first_lane] or LANES
    second_lane = rng.choice(second_candidates)

    return [
        make_event(base_times[0], first_lane, "Wall", 0.0),
        make_event(base_times[1], second_lane, "Aerial", 0.0),
    ]


def phrase_wall_pair(base_times, lane_history, rng):
    first_lane = rng.choice(valid_next_lane(LANES, lane_history))
    second_candidates = [l for l in LANES if l != first_lane] or LANES
    second_lane = rng.choice(valid_next_lane(second_candidates, lane_history + [first_lane]))

    return [
        make_event(base_times[0], first_lane, "Wall", 0.0),
        make_event(base_times[1], second_lane, "Wall", 0.0),
    ]


def phrase_center_hold(base_times, lane_history, rng):
    lane = rng.choice(valid_next_lane([0, -1, 1], lane_history))
    duration = rng.uniform(1.1, 2.2)
    return [
        make_event(base_times[0], lane, "Wall", duration),
        make_event(base_times[1], 0 if lane != 0 else rng.choice([-1, 1]), "Wall", 0.0),
    ]


def apply_lane_history_constraints(pattern, lane_history):
    """
    Adjust a fixed lane pattern if it would create >2 repeats in a row.
    """
    result = []
    hist = lane_history[:]

    for lane in pattern:
        candidates = valid_next_lane(LANES, hist)
        chosen = lane if lane in candidates else random.choice(candidates)
        result.append(chosen)
        hist.append(chosen)

    return result


def filter_and_sort_events(events, min_spacing=0.6):
    """
    Ensure ascending order and at least min_spacing seconds between obstacles.
    If two events are too close, keep the earlier one.
    """
    events = sorted(events, key=lambda e: e["time"])
    filtered = []

    last_time = -math.inf
    for event in events:
        t = float(event["time"])
        if t - last_time >= min_spacing:
            filtered.append(event)
            last_time = t

    return filtered


def generate_obstacle_events(beat_times: np.ndarray, rng: random.Random):
    stride = choose_stride(beat_times)
    selected_times = beat_times[::stride]

    if len(selected_times) < 2:
        return []

    events = []
    lane_history = []

    i = 0
    while i < len(selected_times):
        remaining = len(selected_times) - i

        # Prefer 2–4 beat phrases
        phrase_options = []

        if remaining >= 2:
            phrase_options.extend([
                ("wall_pair", 0.34, 2),
                ("center_left", 0.12, 2),
                ("aerial_mix", 0.12, 2),
                ("long_wall_then_gap", 0.10, 2),
                ("center_hold", 0.10, 2),
            ])

        if remaining >= 3:
            phrase_options.extend([
                ("left_center_right", 0.13, 3),
                ("right_center_left", 0.09, 3),
            ])

        # Normalize weights
        total_weight = sum(weight for _, weight, _ in phrase_options)
        roll = rng.random() * total_weight

        chosen_name = None
        chosen_len = None
        acc = 0.0
        for name, weight, length in phrase_options:
            acc += weight
            if roll <= acc:
                chosen_name = name
                chosen_len = length
                break

        if chosen_name is None:
            chosen_name, _, chosen_len = phrase_options[-1]

        base_times = selected_times[i:i + chosen_len]

        if chosen_name == "center_left":
            phrase_events = phrase_center_left(base_times, lane_history)
        elif chosen_name == "left_center_right":
            phrase_events = phrase_left_center_right(base_times, lane_history)
        elif chosen_name == "right_center_left":
            phrase_events = phrase_right_center_left(base_times, lane_history)
        elif chosen_name == "long_wall_then_gap":
            phrase_events = phrase_long_wall_then_gap(base_times, lane_history, rng)
        elif chosen_name == "aerial_mix":
            phrase_events = phrase_aerial_mix(base_times, lane_history, rng)
        elif chosen_name == "center_hold":
            phrase_events = phrase_center_hold(base_times, lane_history, rng)
        else:
            phrase_events = phrase_wall_pair(base_times, lane_history, rng)

        for event in phrase_events:
            lane_history.append(event["lane"])

        events.extend(phrase_events)
        i += chosen_len

    return filter_and_sort_events(events, min_spacing=0.6)


def analyze_song(song_path: Path):
    """
    Load the song and detect beat timestamps with librosa.
    """
    # sr=None preserves native sample rate
    y, sr = librosa.load(song_path, sr=None, mono=True)

    # Optional light trim of silence to improve beat tracking
    y, _ = librosa.effects.trim(y, top_db=25)

    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)

    # tempo may come back as ndarray in some cases
    if isinstance(tempo, np.ndarray):
        tempo = float(np.squeeze(tempo))
    else:
        tempo = float(tempo)

    beat_times = librosa.frames_to_time(beat_frames, sr=sr)

    return tempo, beat_times


def build_level_data(song_path: Path, scroll_speed=None, seed=42):
    rng = random.Random(seed)

    bpm, beat_times = analyze_song(song_path)
    events = generate_obstacle_events(beat_times, rng)

    song_name = song_path.stem
    if scroll_speed is None:
        scroll_speed = estimate_scroll_speed(bpm)

    level_data = {
        "songName": song_name,
        "bpm": round(float(bpm), 3),
        "scrollSpeed": float(scroll_speed),
        "events": events,
    }

    return level_data


def export_level_json(level_data: dict, output_dir: Path):
    output_dir.mkdir(parents=True, exist_ok=True)
    song_name = level_data["songName"]
    output_path = output_dir / f"level_{song_name}.json"

    with output_path.open("w", encoding="utf-8") as f:
        json.dump(level_data, f, indent=2)

    return output_path


def main():
    parser = argparse.ArgumentParser(
        description="Analyze a song with librosa and export endless-runner obstacle JSON."
    )
    parser.add_argument("song", type=str, help="Path to the input .wav or .mp3 file")
    parser.add_argument(
        "--output-dir",
        type=str,
        default=".",
        help="Directory where the JSON file will be saved"
    )
    parser.add_argument(
        "--scroll-speed",
        type=float,
        default=None,
        help="Override scroll speed. If omitted, it is estimated from BPM."
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=42,
        help="Random seed for reproducible generation"
    )

    args = parser.parse_args()

    song_path = Path(args.song)
    if not song_path.exists():
        raise FileNotFoundError(f"Song file not found: {song_path}")

    if song_path.suffix.lower() not in {".wav", ".mp3"}:
        raise ValueError("Input file must be a .wav or .mp3")

    level_data = build_level_data(
        song_path=song_path,
        scroll_speed=args.scroll_speed,
        seed=args.seed,
    )

    output_path = export_level_json(level_data, Path(args.output_dir))

    print(f"Done.")
    print(f"Song: {level_data['songName']}")
    print(f"BPM: {level_data['bpm']}")
    print(f"Events: {len(level_data['events'])}")
    print(f"Output: {output_path}")


if __name__ == "__main__":
    main()