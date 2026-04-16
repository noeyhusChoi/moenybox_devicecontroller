from __future__ import annotations

import math
import random
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 44_100
TARGET_PEAK = 0.78
OUTPUT_DIR = Path("Kiosk/Assets/Sound")


def sine(phase: float) -> float:
    return math.sin(phase)


def triangle(phase: float) -> float:
    return 2.0 / math.pi * math.asin(math.sin(phase))


def square_soft(phase: float) -> float:
    return math.tanh(3.4 * math.sin(phase))


def saw_soft(phase: float) -> float:
    total = 0.0
    for harmonic in range(1, 6):
        total += math.sin(harmonic * phase) / harmonic
    return total / 1.875


WAVEFORMS = {
    "sine": sine,
    "triangle": triangle,
    "square_soft": square_soft,
    "saw_soft": saw_soft,
}


PRESETS = [
    {
        "file_name": "ButtonClick01_SoftTick.wav",
        "label": "SoftTick",
        "description": "Soft high tick with short tail.",
        "duration_ms": 78,
        "tone": {"wave": "triangle", "freq": 2050, "sweep": -1800, "decay_ms": 26, "gain": 0.48},
        "overtone": {"wave": "sine", "freq": 3350, "sweep": -1200, "decay_ms": 20, "gain": 0.13},
        "noise": {"gain": 0.11, "decay_ms": 14, "lowpass_hz": 8200, "highpass_hz": 2200},
        "impulses": [(0.0, 0.22, 0.42)],
    },
    {
        "file_name": "ButtonClick02_BrightTick.wav",
        "label": "BrightTick",
        "description": "Sharper UI tick with more top end.",
        "duration_ms": 72,
        "tone": {"wave": "sine", "freq": 2480, "sweep": -2400, "decay_ms": 24, "gain": 0.44},
        "overtone": {"wave": "triangle", "freq": 3960, "sweep": -2000, "decay_ms": 16, "gain": 0.15},
        "noise": {"gain": 0.13, "decay_ms": 10, "lowpass_hz": 12000, "highpass_hz": 3600},
        "impulses": [(0.0, 0.18, 0.48)],
    },
    {
        "file_name": "ButtonClick03_PlasticTap.wav",
        "label": "PlasticTap",
        "description": "Rounded plastic key tap.",
        "duration_ms": 88,
        "tone": {"wave": "square_soft", "freq": 1480, "sweep": -900, "decay_ms": 34, "gain": 0.43},
        "overtone": {"wave": "triangle", "freq": 2760, "sweep": -600, "decay_ms": 18, "gain": 0.10},
        "noise": {"gain": 0.09, "decay_ms": 18, "lowpass_hz": 5600, "highpass_hz": 900},
        "impulses": [(0.0, 0.26, 0.36)],
    },
    {
        "file_name": "ButtonClick04_PlasticSnap.wav",
        "label": "PlasticSnap",
        "description": "Crisper version of a plastic button press.",
        "duration_ms": 82,
        "tone": {"wave": "square_soft", "freq": 1760, "sweep": -1600, "decay_ms": 28, "gain": 0.47},
        "overtone": {"wave": "sine", "freq": 3120, "sweep": -1100, "decay_ms": 18, "gain": 0.12},
        "noise": {"gain": 0.12, "decay_ms": 12, "lowpass_hz": 7600, "highpass_hz": 1800},
        "impulses": [(0.0, 0.20, 0.46)],
    },
    {
        "file_name": "ButtonClick05_GentlePop.wav",
        "label": "GentlePop",
        "description": "Soft pop for confirm and secondary actions.",
        "duration_ms": 96,
        "tone": {"wave": "sine", "freq": 980, "sweep": -650, "decay_ms": 42, "gain": 0.38},
        "overtone": {"wave": "triangle", "freq": 2040, "sweep": -900, "decay_ms": 22, "gain": 0.08},
        "sub": {"wave": "sine", "freq": 290, "sweep": -60, "decay_ms": 52, "gain": 0.07},
        "noise": {"gain": 0.05, "decay_ms": 20, "lowpass_hz": 3200, "highpass_hz": 400},
        "impulses": [(0.0, 0.28, 0.26)],
    },
    {
        "file_name": "ButtonClick06_CleanTap.wav",
        "label": "CleanTap",
        "description": "Neutral click without aggressive attack.",
        "duration_ms": 84,
        "tone": {"wave": "triangle", "freq": 1620, "sweep": -1100, "decay_ms": 30, "gain": 0.41},
        "overtone": {"wave": "sine", "freq": 2840, "sweep": -600, "decay_ms": 20, "gain": 0.08},
        "noise": {"gain": 0.08, "decay_ms": 14, "lowpass_hz": 6200, "highpass_hz": 1300},
        "impulses": [(0.0, 0.20, 0.32)],
    },
    {
        "file_name": "ButtonClick07_CrispSnap.wav",
        "label": "CrispSnap",
        "description": "Fast mechanical snap for primary buttons.",
        "duration_ms": 74,
        "tone": {"wave": "square_soft", "freq": 1920, "sweep": -2400, "decay_ms": 24, "gain": 0.46},
        "overtone": {"wave": "triangle", "freq": 3660, "sweep": -1800, "decay_ms": 15, "gain": 0.16},
        "noise": {"gain": 0.16, "decay_ms": 10, "lowpass_hz": 9800, "highpass_hz": 2600},
        "impulses": [(0.0, 0.16, 0.56)],
    },
    {
        "file_name": "ButtonClick08_LiteThock.wav",
        "label": "LiteThock",
        "description": "Small low-end thock with restrained brightness.",
        "duration_ms": 108,
        "tone": {"wave": "triangle", "freq": 720, "sweep": -520, "decay_ms": 46, "gain": 0.33},
        "overtone": {"wave": "sine", "freq": 1480, "sweep": -700, "decay_ms": 22, "gain": 0.08},
        "sub": {"wave": "sine", "freq": 210, "sweep": -50, "decay_ms": 64, "gain": 0.11},
        "noise": {"gain": 0.05, "decay_ms": 18, "lowpass_hz": 2600, "highpass_hz": 300},
        "impulses": [(0.0, 0.28, 0.22)],
    },
    {
        "file_name": "ButtonClick09_LowTock.wav",
        "label": "LowTock",
        "description": "Compact low tap that still reads as a click.",
        "duration_ms": 102,
        "tone": {"wave": "saw_soft", "freq": 840, "sweep": -420, "decay_ms": 42, "gain": 0.35},
        "overtone": {"wave": "triangle", "freq": 1720, "sweep": -500, "decay_ms": 18, "gain": 0.08},
        "sub": {"wave": "sine", "freq": 240, "sweep": -40, "decay_ms": 60, "gain": 0.09},
        "noise": {"gain": 0.06, "decay_ms": 16, "lowpass_hz": 3400, "highpass_hz": 500},
        "impulses": [(0.0, 0.24, 0.26)],
    },
    {
        "file_name": "ButtonClick10_HollowTap.wav",
        "label": "HollowTap",
        "description": "Midrange hollow tap with light resonance.",
        "duration_ms": 92,
        "tone": {"wave": "saw_soft", "freq": 1240, "sweep": -920, "decay_ms": 36, "gain": 0.37},
        "overtone": {"wave": "sine", "freq": 2280, "sweep": -420, "decay_ms": 20, "gain": 0.07},
        "noise": {"gain": 0.07, "decay_ms": 16, "lowpass_hz": 4800, "highpass_hz": 900},
        "impulses": [(0.0, 0.22, 0.30)],
    },
    {
        "file_name": "ButtonClick11_GlassTick.wav",
        "label": "GlassTick",
        "description": "Tiny bright tick with glassy top end.",
        "duration_ms": 68,
        "tone": {"wave": "sine", "freq": 2960, "sweep": -2800, "decay_ms": 22, "gain": 0.40},
        "overtone": {"wave": "sine", "freq": 4820, "sweep": -2600, "decay_ms": 12, "gain": 0.14},
        "noise": {"gain": 0.10, "decay_ms": 8, "lowpass_hz": 14000, "highpass_hz": 4200},
        "impulses": [(0.0, 0.14, 0.50)],
    },
    {
        "file_name": "ButtonClick12_RubberTap.wav",
        "label": "RubberTap",
        "description": "Muted and soft, useful for dense screens.",
        "duration_ms": 100,
        "tone": {"wave": "triangle", "freq": 1080, "sweep": -760, "decay_ms": 40, "gain": 0.34},
        "overtone": {"wave": "sine", "freq": 2160, "sweep": -500, "decay_ms": 18, "gain": 0.06},
        "noise": {"gain": 0.04, "decay_ms": 20, "lowpass_hz": 2400, "highpass_hz": 220},
        "impulses": [(0.0, 0.30, 0.18)],
    },
    {
        "file_name": "ButtonClick13_CardTap.wav",
        "label": "CardTap",
        "description": "Polite card-reader style tap.",
        "duration_ms": 86,
        "tone": {"wave": "triangle", "freq": 1560, "sweep": -980, "decay_ms": 30, "gain": 0.39},
        "overtone": {"wave": "sine", "freq": 2980, "sweep": -900, "decay_ms": 14, "gain": 0.09},
        "noise": {"gain": 0.09, "decay_ms": 12, "lowpass_hz": 7000, "highpass_hz": 1500},
        "impulses": [(0.0, 0.20, 0.34)],
    },
    {
        "file_name": "ButtonClick14_DigitalTap.wav",
        "label": "DigitalTap",
        "description": "Short digital tap that still feels tactile.",
        "duration_ms": 70,
        "tone": {"wave": "square_soft", "freq": 2320, "sweep": -800, "decay_ms": 20, "gain": 0.37},
        "overtone": {"wave": "sine", "freq": 3480, "sweep": -600, "decay_ms": 12, "gain": 0.11},
        "noise": {"gain": 0.08, "decay_ms": 9, "lowpass_hz": 9000, "highpass_hz": 2100},
        "impulses": [(0.0, 0.18, 0.40)],
    },
    {
        "file_name": "ButtonClick15_MechKey.wav",
        "label": "MechKey",
        "description": "Compact keyboard-like mechanical key click.",
        "duration_ms": 92,
        "tone": {"wave": "square_soft", "freq": 1320, "sweep": -700, "decay_ms": 32, "gain": 0.37},
        "overtone": {"wave": "triangle", "freq": 2540, "sweep": -900, "decay_ms": 18, "gain": 0.10},
        "noise": {"gain": 0.12, "decay_ms": 12, "lowpass_hz": 7200, "highpass_hz": 1700},
        "impulses": [(0.0, 0.18, 0.46), (18.0, 0.22, 0.18)],
    },
    {
        "file_name": "ButtonClick16_ShortConfirm.wav",
        "label": "ShortConfirm",
        "description": "Friendly click for confirm states.",
        "duration_ms": 90,
        "tone": {"wave": "sine", "freq": 1180, "sweep": 280, "decay_ms": 38, "gain": 0.36},
        "overtone": {"wave": "triangle", "freq": 2360, "sweep": 120, "decay_ms": 20, "gain": 0.08},
        "sub": {"wave": "sine", "freq": 310, "sweep": 0, "decay_ms": 56, "gain": 0.05},
        "noise": {"gain": 0.05, "decay_ms": 14, "lowpass_hz": 5200, "highpass_hz": 1000},
        "impulses": [(0.0, 0.22, 0.22)],
    },
    {
        "file_name": "ButtonClick17_TinyPulse.wav",
        "label": "TinyPulse",
        "description": "Very short pulse with a clean leading edge.",
        "duration_ms": 62,
        "tone": {"wave": "sine", "freq": 1840, "sweep": -1400, "decay_ms": 18, "gain": 0.34},
        "overtone": {"wave": "sine", "freq": 3220, "sweep": -1600, "decay_ms": 10, "gain": 0.09},
        "noise": {"gain": 0.08, "decay_ms": 7, "lowpass_hz": 10500, "highpass_hz": 3000},
        "impulses": [(0.0, 0.16, 0.44)],
    },
    {
        "file_name": "ButtonClick18_UIBlip.wav",
        "label": "UIBlip",
        "description": "Blippy UI tap without turning into a notification.",
        "duration_ms": 76,
        "tone": {"wave": "sine", "freq": 1460, "sweep": 420, "decay_ms": 24, "gain": 0.35},
        "overtone": {"wave": "triangle", "freq": 2840, "sweep": -300, "decay_ms": 12, "gain": 0.09},
        "noise": {"gain": 0.06, "decay_ms": 9, "lowpass_hz": 8000, "highpass_hz": 2000},
        "impulses": [(0.0, 0.16, 0.30)],
    },
    {
        "file_name": "ButtonClick19_RoundClick.wav",
        "label": "RoundClick",
        "description": "Round mid-focused click with no harsh tail.",
        "duration_ms": 94,
        "tone": {"wave": "triangle", "freq": 1360, "sweep": -620, "decay_ms": 36, "gain": 0.38},
        "overtone": {"wave": "saw_soft", "freq": 2280, "sweep": -300, "decay_ms": 18, "gain": 0.06},
        "noise": {"gain": 0.07, "decay_ms": 14, "lowpass_hz": 5200, "highpass_hz": 900},
        "impulses": [(0.0, 0.22, 0.28)],
    },
    {
        "file_name": "ButtonClick20_DampClick.wav",
        "label": "DampClick",
        "description": "Damped UI press for screens with lots of repetition.",
        "duration_ms": 88,
        "tone": {"wave": "sine", "freq": 1220, "sweep": -800, "decay_ms": 34, "gain": 0.34},
        "overtone": {"wave": "triangle", "freq": 2120, "sweep": -420, "decay_ms": 14, "gain": 0.05},
        "noise": {"gain": 0.04, "decay_ms": 12, "lowpass_hz": 3000, "highpass_hz": 300},
        "impulses": [(0.0, 0.24, 0.18)],
    },
    {
        "file_name": "ButtonClick21_SoftKnock.wav",
        "label": "SoftKnock",
        "description": "Small woody knock leaning low-mid.",
        "duration_ms": 104,
        "tone": {"wave": "saw_soft", "freq": 910, "sweep": -440, "decay_ms": 44, "gain": 0.34},
        "overtone": {"wave": "triangle", "freq": 1760, "sweep": -260, "decay_ms": 18, "gain": 0.06},
        "sub": {"wave": "sine", "freq": 250, "sweep": -20, "decay_ms": 58, "gain": 0.07},
        "noise": {"gain": 0.04, "decay_ms": 16, "lowpass_hz": 2600, "highpass_hz": 280},
        "impulses": [(0.0, 0.26, 0.20)],
    },
    {
        "file_name": "ButtonClick22_DustTap.wav",
        "label": "DustTap",
        "description": "Dry dusty transient with very little ring.",
        "duration_ms": 80,
        "tone": {"wave": "triangle", "freq": 1540, "sweep": -1400, "decay_ms": 24, "gain": 0.29},
        "overtone": {"wave": "sine", "freq": 2620, "sweep": -800, "decay_ms": 10, "gain": 0.05},
        "noise": {"gain": 0.12, "decay_ms": 10, "lowpass_hz": 5600, "highpass_hz": 2400},
        "impulses": [(0.0, 0.18, 0.24)],
    },
    {
        "file_name": "ButtonClick23_BrightPing.wav",
        "label": "BrightPing",
        "description": "Ping-like edge but cut short for button use.",
        "duration_ms": 84,
        "tone": {"wave": "sine", "freq": 2140, "sweep": 180, "decay_ms": 28, "gain": 0.34},
        "overtone": {"wave": "sine", "freq": 4280, "sweep": -500, "decay_ms": 14, "gain": 0.13},
        "noise": {"gain": 0.07, "decay_ms": 8, "lowpass_hz": 12000, "highpass_hz": 3400},
        "impulses": [(0.0, 0.16, 0.38)],
    },
    {
        "file_name": "ButtonClick24_MutedPop.wav",
        "label": "MutedPop",
        "description": "Low-volume feeling pop for accessibility-heavy flows.",
        "duration_ms": 90,
        "tone": {"wave": "sine", "freq": 960, "sweep": -460, "decay_ms": 38, "gain": 0.30},
        "overtone": {"wave": "triangle", "freq": 1960, "sweep": -220, "decay_ms": 16, "gain": 0.05},
        "sub": {"wave": "sine", "freq": 260, "sweep": 0, "decay_ms": 54, "gain": 0.04},
        "noise": {"gain": 0.03, "decay_ms": 12, "lowpass_hz": 2600, "highpass_hz": 180},
        "impulses": [(0.0, 0.26, 0.16)],
    },
    {
        "file_name": "ButtonClick25_DeepTap.wav",
        "label": "DeepTap",
        "description": "Low-weight tap that still stays short.",
        "duration_ms": 112,
        "tone": {"wave": "triangle", "freq": 680, "sweep": -340, "decay_ms": 50, "gain": 0.33},
        "overtone": {"wave": "sine", "freq": 1420, "sweep": -200, "decay_ms": 20, "gain": 0.05},
        "sub": {"wave": "sine", "freq": 190, "sweep": 0, "decay_ms": 68, "gain": 0.11},
        "noise": {"gain": 0.04, "decay_ms": 18, "lowpass_hz": 2200, "highpass_hz": 220},
        "impulses": [(0.0, 0.26, 0.18)],
    },
    {
        "file_name": "ButtonClick26_QuickTock.wav",
        "label": "QuickTock",
        "description": "Small tock with slightly brighter initial hit.",
        "duration_ms": 86,
        "tone": {"wave": "saw_soft", "freq": 1100, "sweep": -860, "decay_ms": 30, "gain": 0.36},
        "overtone": {"wave": "triangle", "freq": 2300, "sweep": -880, "decay_ms": 14, "gain": 0.07},
        "noise": {"gain": 0.08, "decay_ms": 11, "lowpass_hz": 6800, "highpass_hz": 1500},
        "impulses": [(0.0, 0.20, 0.30)],
    },
    {
        "file_name": "ButtonClick27_DoubleTick.wav",
        "label": "DoubleTick",
        "description": "Two-step click that stays subtle.",
        "duration_ms": 120,
        "tone": {"wave": "triangle", "freq": 1680, "sweep": -1200, "decay_ms": 26, "gain": 0.34},
        "overtone": {"wave": "sine", "freq": 3040, "sweep": -900, "decay_ms": 14, "gain": 0.08},
        "noise": {"gain": 0.07, "decay_ms": 10, "lowpass_hz": 8200, "highpass_hz": 2200},
        "impulses": [(0.0, 0.16, 0.36), (24.0, 0.16, 0.20)],
        "secondary_tone": {"delay_ms": 22, "gain_scale": 0.52},
    },
    {
        "file_name": "ButtonClick28_TinySwitch.wav",
        "label": "TinySwitch",
        "description": "Very compact switch-like click.",
        "duration_ms": 66,
        "tone": {"wave": "square_soft", "freq": 1860, "sweep": -1700, "decay_ms": 20, "gain": 0.33},
        "overtone": {"wave": "triangle", "freq": 3400, "sweep": -1500, "decay_ms": 10, "gain": 0.08},
        "noise": {"gain": 0.10, "decay_ms": 8, "lowpass_hz": 8800, "highpass_hz": 2600},
        "impulses": [(0.0, 0.14, 0.42)],
    },
    {
        "file_name": "ButtonClick29_SmoothClick.wav",
        "label": "SmoothClick",
        "description": "Balanced click for general-purpose UI use.",
        "duration_ms": 90,
        "tone": {"wave": "triangle", "freq": 1440, "sweep": -780, "decay_ms": 34, "gain": 0.38},
        "overtone": {"wave": "sine", "freq": 2840, "sweep": -520, "decay_ms": 16, "gain": 0.08},
        "noise": {"gain": 0.07, "decay_ms": 12, "lowpass_hz": 6200, "highpass_hz": 1100},
        "impulses": [(0.0, 0.20, 0.28)],
    },
    {
        "file_name": "ButtonClick30_SparkTap.wav",
        "label": "SparkTap",
        "description": "Bright, lively tap for prominent actions.",
        "duration_ms": 78,
        "tone": {"wave": "sine", "freq": 2280, "sweep": -1500, "decay_ms": 24, "gain": 0.39},
        "overtone": {"wave": "triangle", "freq": 4120, "sweep": -1800, "decay_ms": 12, "gain": 0.12},
        "noise": {"gain": 0.11, "decay_ms": 8, "lowpass_hz": 13000, "highpass_hz": 3200},
        "impulses": [(0.0, 0.14, 0.46)],
    },
]


def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def lowpass(samples: list[float], cutoff_hz: float) -> list[float]:
    if cutoff_hz <= 0:
        return samples[:]
    alpha = math.exp(-2.0 * math.pi * cutoff_hz / SAMPLE_RATE)
    output: list[float] = []
    acc = 0.0
    for sample in samples:
        acc = alpha * acc + (1.0 - alpha) * sample
        output.append(acc)
    return output


def highpass(samples: list[float], cutoff_hz: float) -> list[float]:
    if cutoff_hz <= 0:
        return samples[:]
    low = lowpass(samples, cutoff_hz)
    return [sample - low_sample for sample, low_sample in zip(samples, low)]


def apply_component(
    buffer: list[float],
    component: dict[str, float] | None,
    *,
    start_ms: float = 0.0,
    gain_scale: float = 1.0,
) -> None:
    if not component:
        return

    wave_fn = WAVEFORMS[component["wave"]]
    start_index = int(start_ms / 1000.0 * SAMPLE_RATE)
    if start_index >= len(buffer):
        return

    freq = float(component["freq"])
    sweep = float(component.get("sweep", 0.0))
    attack_seconds = float(component.get("attack_ms", 0.6)) / 1000.0
    decay_seconds = max(0.001, float(component["decay_ms"]) / 1000.0)
    gain = float(component["gain"]) * gain_scale

    for index in range(start_index, len(buffer)):
        elapsed = (index - start_index) / SAMPLE_RATE
        attack = 1.0 if elapsed >= attack_seconds else elapsed / attack_seconds
        envelope = math.exp(-elapsed / decay_seconds) * attack
        phase = 2.0 * math.pi * (freq * elapsed + 0.5 * sweep * elapsed * elapsed)
        buffer[index] += gain * envelope * wave_fn(phase)


def apply_noise(buffer: list[float], noise: dict[str, float] | None, rng: random.Random) -> None:
    if not noise:
        return

    decay_seconds = max(0.001, float(noise["decay_ms"]) / 1000.0)
    raw = [rng.uniform(-1.0, 1.0) for _ in buffer]
    lowpass_hz = float(noise.get("lowpass_hz", 0.0))
    highpass_hz = float(noise.get("highpass_hz", 0.0))

    shaped = raw
    if lowpass_hz > 0:
        shaped = lowpass(shaped, lowpass_hz)
    if highpass_hz > 0:
        shaped = highpass(shaped, highpass_hz)

    gain = float(noise["gain"])
    for index, sample in enumerate(shaped):
        elapsed = index / SAMPLE_RATE
        envelope = math.exp(-elapsed / decay_seconds)
        buffer[index] += gain * envelope * sample


def apply_impulses(buffer: list[float], impulses: list[tuple[float, float, float]] | None) -> None:
    if not impulses:
        return

    for position_ms, width_ms, gain in impulses:
        start = int(position_ms / 1000.0 * SAMPLE_RATE)
        width = max(2, int(width_ms / 1000.0 * SAMPLE_RATE))
        for offset in range(width):
            index = start + offset
            if index >= len(buffer):
                break
            progress = offset / width
            pulse = (1.0 - progress) ** 3
            buffer[index] += gain * pulse


def finalize(buffer: list[float]) -> bytes:
    if not buffer:
        return b""

    mean = sum(buffer) / len(buffer)
    centered = [sample - mean for sample in buffer]

    fade_samples = max(4, int(0.004 * SAMPLE_RATE))
    for index in range(min(fade_samples, len(centered))):
        centered[-1 - index] *= index / fade_samples

    saturated = [math.tanh(sample * 1.35) for sample in centered]
    peak = max(abs(sample) for sample in saturated) or 1.0
    normalized = [sample * (TARGET_PEAK / peak) for sample in saturated]

    frames = bytearray()
    for sample in normalized:
        pcm = int(clamp(sample, -1.0, 1.0) * 32767)
        frames.extend(struct.pack("<h", pcm))
    return bytes(frames)


def synthesize_preset(preset: dict[str, object], seed_index: int) -> bytes:
    duration_ms = float(preset["duration_ms"])
    samples = max(1, int(duration_ms / 1000.0 * SAMPLE_RATE))
    buffer = [0.0] * samples

    apply_component(buffer, preset.get("tone"))
    apply_component(buffer, preset.get("overtone"))
    apply_component(buffer, preset.get("sub"))

    secondary_tone = preset.get("secondary_tone")
    if secondary_tone:
        apply_component(
            buffer,
            preset.get("tone"),
            start_ms=float(secondary_tone["delay_ms"]),
            gain_scale=float(secondary_tone["gain_scale"]),
        )

    rng = random.Random(4_102 + seed_index * 97)
    apply_noise(buffer, preset.get("noise"), rng)
    apply_impulses(buffer, preset.get("impulses"))
    return finalize(buffer)


def write_wave(path: Path, frames: bytes) -> None:
    with wave.open(str(path), "wb") as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(SAMPLE_RATE)
        wav_file.writeframes(frames)


def build_catalog() -> str:
    lines = [
        "# Button Click Sound Catalog",
        "",
        "Generated by `tools/generate_button_click_sounds.py`.",
        "",
        "| File | Character |",
        "| --- | --- |",
    ]
    for preset in PRESETS:
        lines.append(f"| `{preset['file_name']}` | {preset['description']} |")
    lines.append("")
    return "\n".join(lines)


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for index, preset in enumerate(PRESETS, start=1):
        frames = synthesize_preset(preset, index)
        write_wave(OUTPUT_DIR / preset["file_name"], frames)

    catalog_path = OUTPUT_DIR / "ButtonClickCatalog.md"
    catalog_path.write_text(build_catalog(), encoding="utf-8")

    print(f"Generated {len(PRESETS)} button click sounds in {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
