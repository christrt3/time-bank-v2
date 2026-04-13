/* ============================================================
   Alachua Community Collective — Site JavaScript
   Accessibility: text resize, text-to-speech, high contrast
   ============================================================ */

// ── Text Size ─────────────────────────────────────────────────
const FONT_KEY = 'acc_font_size';
const MIN_SIZE = 12, MAX_SIZE = 24, DEFAULT_SIZE = 16;

function accFontSize(delta) {
    const current = parseInt(localStorage.getItem(FONT_KEY) || DEFAULT_SIZE);
    const next = Math.min(MAX_SIZE, Math.max(MIN_SIZE, current + delta));
    document.documentElement.style.fontSize = next + 'px';
    localStorage.setItem(FONT_KEY, next);
}

// Apply saved font size on page load
(function () {
    const saved = localStorage.getItem(FONT_KEY);
    if (saved) document.documentElement.style.fontSize = saved + 'px';
})();

// ── High Contrast ─────────────────────────────────────────────
const CONTRAST_KEY = 'acc_high_contrast';

function accHighContrast() {
    const isOn = document.body.classList.toggle('high-contrast');
    localStorage.setItem(CONTRAST_KEY, isOn ? '1' : '0');
}

(function () {
    if (localStorage.getItem(CONTRAST_KEY) === '1') {
        document.body.classList.add('high-contrast');
    }
})();

// ── Text-to-Speech ────────────────────────────────────────────
let ttsEnabled = false;
let ttsUtterance = null;

function accToggleTTS() {
    ttsEnabled = !ttsEnabled;
    const btn = document.getElementById('tts-btn');
    if (btn) {
        btn.classList.toggle('btn-light', ttsEnabled);
        btn.classList.toggle('btn-outline-light', !ttsEnabled);
        btn.title = ttsEnabled ? 'Text-to-speech ON (click to disable)' : 'Toggle text-to-speech';
    }

    if (!ttsEnabled) {
        window.speechSynthesis.cancel();
    } else {
        showTTSNotice();
    }
}

function showTTSNotice() {
    if (!ttsEnabled) return;
    const msg = new SpeechSynthesisUtterance(
        'Text to speech is enabled. Hover over any text to hear it read aloud.'
    );
    msg.lang = document.documentElement.lang === 'es' ? 'es-US' : 'en-US';
    window.speechSynthesis.speak(msg);
}

// Read text on hover when TTS is enabled
document.addEventListener('mouseover', function (e) {
    if (!ttsEnabled) return;
    const target = e.target;
    const text = target.innerText?.trim();
    if (!text || text.length < 3 || target.tagName === 'SCRIPT') return;

    if (ttsUtterance) window.speechSynthesis.cancel();

    // Highlight
    document.querySelectorAll('.tts-active').forEach(el => el.classList.remove('tts-active'));
    target.classList.add('tts-active');

    ttsUtterance = new SpeechSynthesisUtterance(text);
    ttsUtterance.lang = document.documentElement.lang === 'es' ? 'es-US' : 'en-US';
    ttsUtterance.rate = 0.9;
    ttsUtterance.onend = () => target.classList.remove('tts-active');
    window.speechSynthesis.speak(ttsUtterance);
});

// ── Auto-dismiss alerts ────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert.alert-success, .alert.alert-danger').forEach(function (alert) {
        if (alert.querySelector('.btn-close')) {
            setTimeout(() => {
                const bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            }, 6000);
        }
    });

    // Photo preview
    const photoPicker = document.getElementById('ProfilePicture');
    if (photoPicker) {
        photoPicker.addEventListener('change', function () {
            if (this.files && this.files[0]) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    const preview = document.querySelector('img.rounded-circle[alt]');
                    if (preview) {
                        preview.src = e.target.result;
                    }
                };
                reader.readAsDataURL(this.files[0]);
            }
        });
    }

    // Confirm destructive actions
    document.querySelectorAll('[data-confirm]').forEach(function (el) {
        el.addEventListener('click', function (e) {
            if (!confirm(el.dataset.confirm)) e.preventDefault();
        });
    });
});

// ── Language preference (store in cookie) ─────────────────────
const urlParams = new URLSearchParams(window.location.search);
const lang = urlParams.get('lang');
if (lang) {
    document.cookie = `acc_lang=${lang}; path=/; max-age=31536000`;
}
