// Web Audio API Sound Effects for Family Feud
class FeudAudio {
    constructor() {
        this.ctx = null;
    }

    init() {
        if (!this.ctx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) {
                this.ctx = new AudioContext();
            }
        }
        if (this.ctx && this.ctx.state === 'suspended') {
            this.ctx.resume();
        }
    }

    // Classic bell ding for correct answers
    playDing() {
        this.init();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        osc.type = 'triangle';
        osc.frequency.setValueAtTime(880, now); // A5
        osc.frequency.exponentialRampToValueAtTime(1760, now + 0.08); // Quick slide up to A6

        gain.gain.setValueAtTime(0.4, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 1.2);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 1.2);

        // Add a secondary harmonic chime
        const osc2 = this.ctx.createOscillator();
        const gain2 = this.ctx.createGain();
        osc2.type = 'sine';
        osc2.frequency.setValueAtTime(2637, now); // E7
        gain2.gain.setValueAtTime(0.2, now);
        gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.8);
        osc2.connect(gain2);
        gain2.connect(this.ctx.destination);
        osc2.start(now);
        osc2.stop(now + 0.8);
    }

    // Harsh classic Family Feud strike buzzer (ERRRR!)
    playStrike() {
        this.init();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        
        // Two detuned low sawtooth oscillators
        const osc1 = this.ctx.createOscillator();
        const osc2 = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        osc1.type = 'sawtooth';
        osc2.type = 'sawtooth';

        osc1.frequency.setValueAtTime(120, now);
        osc2.frequency.setValueAtTime(124, now);

        gain.gain.setValueAtTime(0.5, now);
        gain.gain.setValueAtTime(0.5, now + 0.6);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.85);

        osc1.connect(gain);
        osc2.connect(gain);
        gain.connect(this.ctx.destination);

        osc1.start(now);
        osc2.start(now);
        osc1.stop(now + 0.85);
        osc2.stop(now + 0.85);
    }

    // Faceoff Buzzer hit (Phone buzzer)
    playBuzzer() {
        this.init();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        osc.type = 'square';
        osc.frequency.setValueAtTime(440, now);
        osc.frequency.setValueAtTime(880, now + 0.1);

        gain.gain.setValueAtTime(0.4, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.5);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 0.5);
    }

    // Duplicate Answer Buzzer (Short double buzz: "BUZZ-BUZZ!")
    playDuplicate() {
        this.init();
        if (!this.ctx) return;
        this.playStrike();
        setTimeout(() => {
            this.playStrike();
        }, 240);
    }

    // Win Fanfare
    playWin() {
        this.init();
        if (!this.ctx) return;

        const notes = [523.25, 659.25, 783.99, 1046.50]; // C5, E5, G5, C6
        notes.forEach((freq, idx) => {
            const now = this.ctx.currentTime + (idx * 0.12);
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(freq, now);
            gain.gain.setValueAtTime(0.3, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.6);
            osc.connect(gain);
            gain.connect(this.ctx.destination);
            osc.start(now);
            osc.stop(now + 0.6);
        });
    }

    vibrate() {
        if (navigator.vibrate) {
            navigator.vibrate([150, 50, 150]);
        }
    }
}

window.feudAudio = new FeudAudio();
