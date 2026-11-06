"""Synthesizes the soundtrack and sound effects for Barvaz The Dealer (all original, no samples)."""
import numpy as np
from scipy.signal import fftconvolve, butter, sosfilt
import wave, os, subprocess

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), 'out', 'Audio')
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(11)


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def env(n, a, d, sustain=0.0, curve=4.0):
    t = np.arange(n) / SR
    e = np.minimum(1.0, t / max(a, 1e-4))
    dec = np.exp(-np.maximum(0, t - a) * curve / max(d, 1e-4))
    return e * (sustain + (1 - sustain) * dec)


def bp(x, lo, hi, order=2):
    return sosfilt(butter(order, [lo, hi], btype='band', fs=SR, output='sos'), x)


def hp(x, f, order=2):
    return sosfilt(butter(order, f, btype='high', fs=SR, output='sos'), x)


def lp(x, f, order=2):
    return sosfilt(butter(order, f, btype='low', fs=SR, output='sos'), x)


def write_wav(name, x, stereo=False):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) or 1.0
    x = x / peak * 0.89
    data = (x * 32767).astype(np.int16)
    path = os.path.join(OUT, name + '.wav')
    with wave.open(path, 'wb') as w:
        w.setnchannels(2 if stereo else 1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.T.tobytes() if stereo else data.tobytes())
    return path


# ------------------------------------------------------------------ instruments
def rhodes(f, dur, vel=0.5):
    n = int(dur * SR)
    t = np.arange(n) / SR
    tone = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t * 6)
    bell = 0.35 * np.sin(2 * np.pi * 7.0 * f * t) * np.exp(-t * 18)
    trem = 1 + 0.12 * np.sin(2 * np.pi * 4.5 * t)
    return vel * (tone + bell) * env(n, 0.004, dur * 0.9, 0.0, 2.6) * trem


def bass(f, dur, vel=0.8):
    n = int(dur * SR)
    t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * 2 * f * t) + 0.12 * np.sin(2 * np.pi * 3 * f * t)
    pluck = 0.4 * np.sin(2 * np.pi * 4 * f * t) * np.exp(-t * 40)
    return vel * (x + pluck) * env(n, 0.006, dur, 0.15, 3.5)


def vibes(f, dur, vel=0.4):
    n = int(dur * SR)
    t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.2 * np.sin(2 * np.pi * 4 * f * t) * np.exp(-t * 10)
    trem = 1 + 0.25 * np.sin(2 * np.pi * 5.5 * t)
    return vel * x * trem * env(n, 0.003, max(dur, 0.6), 0.0, 3.0)


def ride(vel=0.25):
    n = int(0.6 * SR)
    noise = rng.standard_normal(n)
    x = bp(noise, 5000, 12000) * env(n, 0.001, 0.5, 0.0, 6)
    t = np.arange(n) / SR
    ping = 0.3 * np.sin(2 * np.pi * 3150 * t) * np.exp(-t * 9) + 0.2 * np.sin(2 * np.pi * 4870 * t) * np.exp(-t * 11)
    return vel * (x + ping)


def hat(vel=0.15):
    n = int(0.08 * SR)
    return vel * hp(rng.standard_normal(n), 7000) * env(n, 0.001, 0.05, 0.0, 8)


def brush(vel=0.12, dur=0.22):
    n = int(dur * SR)
    return vel * bp(rng.standard_normal(n), 1500, 6000) * env(n, 0.03, dur, 0.0, 5)


def kick(vel=0.5):
    n = int(0.3 * SR)
    t = np.arange(n) / SR
    f = 55 + 60 * np.exp(-t * 30)
    return vel * np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 12)


def add(buf, x, start, pan=0.0):
    i = int(start * SR)
    if i >= buf.shape[1]:
        return
    j = min(buf.shape[1], i + len(x))
    l = np.sqrt(0.5 * (1 - pan))
    r = np.sqrt(0.5 * (1 + pan))
    buf[0, i:j] += x[: j - i] * l
    buf[1, i:j] += x[: j - i] * r


def reverb_ir(seconds=1.6):
    n = int(seconds * SR)
    t = np.arange(n) / SR
    ir = np.stack([rng.standard_normal(n), rng.standard_normal(n)]) * np.exp(-t * 3.2)
    ir[:, : int(0.012 * SR)] = 0
    return lp(ir, 6000) * 0.12


# ------------------------------------------------------------------ music
def music():
    bpm = 112
    beat = 60 / bpm
    swing = 0.66  # swung 8th position within the beat
    # (root midi, chord intervals) per bar, key of F
    F, D, G, C, A, Bb, E = 41, 38, 43, 36, 45, 46, 40
    maj7 = [0, 4, 7, 11]; dom7 = [0, 4, 7, 10]; min7 = [0, 3, 7, 10]; m7b5 = [0, 3, 6, 10]; six = [0, 4, 7, 9]
    prog = [(F, maj7), (D, dom7), (G, min7), (C, dom7),
            (A, min7), (D, dom7), (G, min7), (C, dom7),
            (Bb, maj7), (Bb + 0, min7), (A, min7), (D, dom7),
            (G, min7), (C, dom7), (F, six), (C, dom7)]
    bars = len(prog)
    loop_len = bars * 4 * beat
    total = loop_len * 2 + 2
    buf = np.zeros((2, int(total * SR)))
    mel_rng = np.random.default_rng(5)

    for rep in range(2):
        t0 = rep * loop_len
        for b, (root, chord) in enumerate(prog):
            bt = t0 + b * 4 * beat
            nroot, nchord = prog[(b + 1) % bars]
            # walking bass: root, chord tone, chord tone, chromatic approach to next root
            notes = [root, root + chord[1], root + chord[2], nroot + (1 if mel_rng.random() < 0.5 else -1)]
            if b % 2 == 1:
                notes[1] = root + chord[2]; notes[2] = root + chord[3] - 12 + 12
            for k, m in enumerate(notes):
                add(buf, bass(hz(m), beat * 0.95, 0.55 if k == 0 else 0.45), bt + k * beat, -0.1)
            # rhodes comp: beat 2 and the swung "and" of 3, voiced around middle C
            voicing = [root + 12 + i for i in chord]
            voicing = [v + 12 if v < 55 else v for v in voicing]
            for hit, length, vel in ((1.0, beat * 1.2, 0.30), (2 + swing, beat * 1.6, 0.25)):
                for v in voicing:
                    add(buf, rhodes(hz(v), length, vel), bt + hit * beat + mel_rng.uniform(0, 0.012), 0.25)
            # drums: swing ride, hat on 2 & 4, soft kick, brush swish
            for k in range(4):
                add(buf, ride(0.16 if k % 2 == 0 else 0.2), bt + k * beat, 0.4)
                if k in (1, 3):
                    add(buf, ride(0.11), bt + (k + swing) * beat, 0.4)
                    add(buf, hat(0.16), bt + k * beat, -0.4)
                    add(buf, brush(0.10), bt + k * beat - 0.03, -0.2)
            add(buf, kick(0.35), bt, 0)
            if b % 4 == 2:
                add(buf, kick(0.25), bt + (2 + swing) * beat, 0)
            # vibes melody: sparse chord tones, two phrases per four bars
            if b % 4 in (0, 2):
                pattern = [(0, 1.0), (1.0, 0.66), (1 + swing, 1.0), (2 + swing, 1.4)] if b % 8 < 4 else [(0.0, 0.66), (swing, 1.0), (2.0, 2.0)]
                for pos, length in pattern:
                    tone = root + 24 + chord[mel_rng.integers(1, 4)]
                    while tone > 84: tone -= 12
                    add(buf, vibes(hz(tone), length * beat, 0.24), bt + pos * beat, -0.3)
            else:
                # answer phrase in the second bar
                for pos in (0.0, swing, 1.0 + swing):
                    tone = root + 24 + chord[mel_rng.integers(0, 4)]
                    while tone > 84: tone -= 12
                    add(buf, vibes(hz(tone), beat * 0.9, 0.2), bt + (pos + 2) * beat, -0.3)

    wet = np.stack([fftconvolve(buf[c], reverb_ir()[c])[: buf.shape[1]] for c in range(2)])
    mix = buf + wet
    # seamless loop: take the second pass (it already contains the first pass's tail)
    s = int(loop_len * SR)
    loop = mix[:, s: 2 * s].copy()
    # gentle bus compression + warmth
    loop = np.tanh(loop / (np.max(np.abs(loop)) * 0.8)) * 0.8
    wav = write_wav('music_lounge', loop, stereo=True)
    ogg = wav.replace('.wav', '.ogg')
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', wav, '-c:a', 'libvorbis', '-q:a', '5', ogg], check=True)
    os.remove(wav)
    print('music loop', round(loop_len, 2), 's')


# ------------------------------------------------------------------ sfx
def sfx():
    def n(sec):
        return int(sec * SR)

    # card deal: airy swish with a soft landing tick
    x = bp(rng.standard_normal(n(0.16)), 1800, 7000) * env(n(0.16), 0.02, 0.12, 0, 5)
    tick = np.zeros(n(0.16)); tick[n(0.11):n(0.11) + 200] = hp(rng.standard_normal(200), 3000) * np.linspace(1, 0, 200) * 0.8
    write_wav('card_deal', x + tick)

    # card flip: snap
    x = hp(rng.standard_normal(n(0.07)), 2500) * env(n(0.07), 0.001, 0.035, 0, 8)
    write_wav('card_flip', x)

    # chip clack: modal resonances
    def clack(pitch=1.0):
        t = np.arange(n(0.09)) / SR
        y = sum(a * np.sin(2 * np.pi * f * pitch * t) * np.exp(-t * d) for f, a, d in ((2450, 1, 60), (4180, 0.7, 75), (6320, 0.45, 90), (1630, 0.3, 50)))
        y[:60] += rng.standard_normal(60) * 0.8
        return y
    write_wav('chip', clack())
    st = np.zeros(n(0.32))
    for k, (dt, p) in enumerate(((0, 1.0), (0.06, 1.07), (0.115, 0.95), (0.17, 1.12), (0.215, 1.03))):
        c = clack(p) * (1 - k * 0.12)
        st[n(dt):n(dt) + len(c)] += c
    write_wav('chips_stack', st)

    # whoosh for the slap swing
    L = n(0.28)
    noise = rng.standard_normal(L)
    out = np.zeros(L)
    for i, (lo, hi) in enumerate(np.linspace([400, 1200], [2500, 7000], 8)):
        seg = slice(i * L // 8, (i + 1) * L // 8)
        out[seg] = bp(noise, lo, hi)[seg]
    write_wav('whoosh', out * env(L, 0.18, 0.1, 0, 3))

    # SLAP: sharp crack + fleshy body + low thump
    L = n(0.45)
    t = np.arange(L) / SR
    crack = hp(rng.standard_normal(L), 2000) * np.exp(-t * 90)
    body = bp(rng.standard_normal(L), 300, 2200) * np.exp(-t * 28)
    thump = np.sin(2 * np.pi * (70 + 80 * np.exp(-t * 40)) * t) * np.exp(-t * 14)
    slap = 1.2 * crack + 0.9 * body + 0.8 * thump
    slap = np.tanh(slap * 2.2)
    write_wav('slap', slap)

    # cartoon dizzy: bird tweets / slide whistle wobble
    L = n(0.9)
    t = np.arange(L) / SR
    f = 1400 + 500 * np.sin(2 * np.pi * 7 * t) - 600 * t
    write_wav('dizzy', 0.5 * np.sin(2 * np.pi * np.cumsum(f) / SR) * env(L, 0.02, 0.8, 0.4, 2))

    # win jingle: bright marimba arpeggio
    def mallet(f, dur=0.5, v=1.0):
        t = np.arange(n(dur)) / SR
        return v * (np.sin(2 * np.pi * f * t) + 0.3 * np.sin(2 * np.pi * 4 * f * t) * np.exp(-t * 25)) * np.exp(-t * 7)
    L = n(1.4)
    w = np.zeros(L)
    for k, m in enumerate((65, 69, 72, 77, 81, 84)):
        y = mallet(hz(m), 0.7, 0.8 + k * 0.05)
        i = n(k * 0.085)
        w[i:i + len(y)] += y[: L - i]
    chord = sum(mallet(hz(m), 0.9, 0.5) for m in (77, 81, 84, 89))
    i = n(0.55); w[i:i + len(chord)] += chord[: L - i]
    write_wav('win', w)

    # coins: sparkly bells
    L = n(0.9)
    c = np.zeros(L)
    for k in range(9):
        y = mallet(hz(88 + rng.integers(-3, 6)), 0.35, 0.5)
        i = n(k * 0.06 + rng.uniform(0, 0.02))
        c[i:i + len(y)] += y[: L - i]
    write_wav('coins', c)

    # lose: sad trombone "wah wah wah wahhh"
    L = n(1.9)
    t = np.arange(L) / SR
    out = np.zeros(L)
    for k, (m, start, dur) in enumerate(((58, 0, 0.32), (57, 0.36, 0.32), (56, 0.72, 0.32), (55, 1.08, 0.8))):
        nn = n(dur); tt = np.arange(nn) / SR
        vib = 1 + (0.012 * np.sin(2 * np.pi * 5.5 * tt) if k == 3 else 0)
        ph = np.cumsum(hz(m) * vib) / SR
        saw = 2 * (ph % 1) - 1
        y = lp(saw, 1400) * env(nn, 0.03, dur, 0.6, 2)
        i = n(start); out[i:i + nn] += y[: L - i]
    write_wav('lose', out)

    # UI click
    L = n(0.04)
    t = np.arange(L) / SR
    write_wav('click', np.sin(2 * np.pi * 1800 * t) * np.exp(-t * 120) + 0.3 * hp(rng.standard_normal(L), 4000) * np.exp(-t * 200))

    # swap: quick reverse swish + snap
    sw = bp(rng.standard_normal(n(0.2)), 1500, 6000) * env(n(0.2), 0.15, 0.05, 0, 6)
    snap = hp(rng.standard_normal(n(0.07)), 2500) * env(n(0.07), 0.001, 0.035, 0, 8)
    write_wav('swap', np.concatenate([sw, snap]))

    # dealer gloat: short "ba-dum-tss"
    L = n(1.0)
    g = np.zeros(L)
    k1 = kick(0.8)
    snare = bp(rng.standard_normal(n(0.2)), 800, 5000) * env(n(0.2), 0.001, 0.15, 0, 6)
    cym = ride(0.9)
    for y, at in ((snare, 0.0), (k1, 0.16), (cym, 0.36)):
        i = n(at); g[i:i + len(y)] += y[: L - i]
    write_wav('rimshot', g)


if __name__ == '__main__':
    sfx()
    music()
    print(sorted(os.listdir(OUT)))
