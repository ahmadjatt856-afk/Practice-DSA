/* Blossom Frontend — connected to ASP.NET Core Web API */

// ── Base URL of your running API ──────────────────────────────────────────────
// Change port if you edited appsettings.json
const API_BASE = 'http://localhost:5050/api';

const STORAGE_KEY = 'blossom_user';   // sessionStorage key for logged-in user

// ── API helper ────────────────────────────────────────────────────────────────
async function apiFetch(path, options = {}) {
    try {
        const res = await fetch(API_BASE + path, {
            headers: { 'Content-Type': 'application/json' },
            ...options
        });
        if (res.status === 204) return {};          // No content
        return await res.json();
    } catch (err) {
        console.error('API error:', err);
        return null;
    }
}

async function checkApiOnline() {
    const data = await apiFetch('/health');
    return data?.status === 'ok';
}

// ── Session helpers ───────────────────────────────────────────────────────────
function getUser() {
    try {
        const raw = sessionStorage.getItem(STORAGE_KEY);
        return raw ? JSON.parse(raw) : null;
    } catch { return null; }
}

function setUser(userId, username) {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ userId, username }));
}

function clearUser() {
    sessionStorage.removeItem(STORAGE_KEY);
}

function requireAuth() {
    const user = getUser();
    if (!user && document.body.dataset.page !== 'auth') {
        window.location.href = 'index.html';
    }
    return user;
}

function showWelcome() {
    const user = getUser();
    const el = document.getElementById('welcomeUser');
    if (el && user) el.textContent = user.username;
}

async function logActivity(action, detail = '') {
    const user = getUser();
    if (!user) return;
    // Fire and forget — don't await, non-blocking
    apiFetch('/activity', {
        method: 'POST',
        body: JSON.stringify({ userId: user.userId, action, detail })
    });
}

function showMessage(id, text, type) {
    const el = document.getElementById(id);
    if (!el) return;
    el.textContent = text;
    el.className = 'form-message ' + (type === 'success' ? 'alert-success' : 'alert-banner');
    el.style.display = 'block';
}

// ── Auth Page ─────────────────────────────────────────────────────────────────
async function initAuth() {
    // Show or hide API status banner
    const banner = document.getElementById('apiStatus');
    const online = await checkApiOnline();
    if (banner) {
        banner.style.display = online ? 'none' : 'block';
        if (!online) banner.textContent = '⚠️ Cannot reach the server. Make sure the API is running on localhost:5050.';
    }

    const loginForm  = document.getElementById('loginForm');
    const signupForm = document.getElementById('signupForm');
    const tabs       = document.querySelectorAll('.auth-tab');

    // Tab switching
    tabs.forEach((tab) => {
        tab.addEventListener('click', () => {
            tabs.forEach((t) => t.classList.remove('active'));
            tab.classList.add('active');
            const mode = tab.dataset.tab;
            loginForm.classList.toggle('hidden',  mode !== 'login');
            signupForm.classList.toggle('hidden', mode !== 'signup');
        });
    });

    // ── LOGIN ──────────────────────────────────────────────────────────────
    if (loginForm) {
        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const username = document.getElementById('loginUsername').value.trim();
            const password = document.getElementById('loginPassword').value;

            if (!username || !password) {
                showMessage('loginMessage', 'Please enter username and password.', 'error');
                return;
            }

            const data = await apiFetch('/auth/login', {
                method: 'POST',
                body: JSON.stringify({ username, password })
            });

            if (!data) {
                showMessage('loginMessage', 'Server unreachable. Please try again.', 'error');
                return;
            }
            if (data.userId) {
                setUser(data.userId, data.username);
                window.location.href = 'home.html';
            } else {
                showMessage('loginMessage', 'Invalid username or password.', 'error');
            }
        });
    }

    // ── SIGN UP ────────────────────────────────────────────────────────────
    if (signupForm) {
        signupForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const username = document.getElementById('signupUsername').value.trim();
            const password = document.getElementById('signupPassword').value;

            if (!username) {
                showMessage('signupMessage', 'Username cannot be empty.', 'error');
                return;
            }
            if (!password || password.length < 4) {
                showMessage('signupMessage', 'Password must be at least 4 characters.', 'error');
                return;
            }

            const data = await apiFetch('/auth/register', {
                method: 'POST',
                body: JSON.stringify({ username, password })
            });

            if (!data) {
                showMessage('signupMessage', 'Server unreachable. Please try again.', 'error');
                return;
            }
            if (data.success) {
                showMessage('signupMessage', 'Account created! You can now log in.', 'success');
                setTimeout(() => document.querySelector('[data-tab="login"]')?.click(), 1500);
            } else {
                showMessage('signupMessage', data.message || 'Signup failed.', 'error');
            }
        });
    }
}

// ── Date Helpers ──────────────────────────────────────────────────────────────
function addDays(d, n) {
    const r = new Date(d);
    r.setDate(r.getDate() + n);
    return r;
}

function fmt(d) {
    return d.toISOString().slice(0, 10);
}

function calcCycle(lastPeriodStr, cycleLen, periodLen) {
    const lastPeriod = new Date(lastPeriodStr + 'T00:00:00');
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const nextPeriod   = addDays(lastPeriod, cycleLen);
    const ovulation    = addDays(nextPeriod, -14);
    const fertileStart = addDays(ovulation, -5);
    const fertileEnd   = addDays(ovulation, 1);
    const periodEnd    = addDays(lastPeriod, periodLen);

    const msPerDay     = 86400000;
    const currentDay   = Math.floor((today - lastPeriod) / msPerDay) % cycleLen + 1;
    const ovulationDayNum = Math.floor((ovulation - lastPeriod) / msPerDay);

    let phase;
    if (currentDay <= periodLen) phase = 'Menstrual';
    else if (currentDay <= ovulationDayNum) phase = 'Follicular';
    else if (Math.abs((today - ovulation) / msPerDay) <= 1) phase = 'Ovulation';
    else phase = 'Luteal';

    const daysLeft = Math.floor((nextPeriod - today) / msPerDay);
    let reminder = '';
    if (daysLeft <= 2 && daysLeft >= 0) {
        reminder = `⚠️ Your next period is expected in ${daysLeft} day(s). Stay prepared.`;
    }

    return {
        nextPeriod:   fmt(nextPeriod),
        ovulation:    fmt(ovulation),
        fertileStart: fmt(fertileStart),
        fertileEnd:   fmt(fertileEnd),
        periodEnd:    fmt(periodEnd),
        phase,
        currentDay,
        daysLeft,
        reminder
    };
}

// ── Cycle Tracker ─────────────────────────────────────────────────────────────
function initCycleTracker() {
    const form = document.getElementById('cycleForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const user = getUser();
        if (!user) return;

        const lastPeriod = document.getElementById('lastPeriod').value;
        const cycleLen   = parseInt(document.getElementById('cycleLength').value, 10);
        const periodLen  = parseInt(document.getElementById('periodLength').value, 10);

        if (!lastPeriod || isNaN(cycleLen) || isNaN(periodLen)) {
            alert('Please fill in all fields.'); return;
        }

        const result = calcCycle(lastPeriod, cycleLen, periodLen);

        // Save to database
        await apiFetch('/cycle', {
            method: 'POST',
            body: JSON.stringify({
                userId: user.userId,
                lastPeriodDate: lastPeriod,
                cycleLengthDays: cycleLen,
                periodLengthDays: periodLen,
                currentPhase: result.phase
            })
        });

        // Display results
        const out = document.getElementById('cycleResult');
        if (out) {
            out.innerHTML = `
                <div class="result-card">
                    <p>📅 <strong>Next Period:</strong> ${result.nextPeriod}</p>
                    <p>🥚 <strong>Ovulation:</strong> ${result.ovulation}</p>
                    <p>🌸 <strong>Fertile Window:</strong> ${result.fertileStart} → ${result.fertileEnd}</p>
                    <p>🔄 <strong>Current Phase:</strong> ${result.phase}</p>
                    ${result.reminder ? `<p class="alert-banner">${result.reminder}</p>` : ''}
                </div>`;
        }
        await logActivity('CYCLE_LOG', `Phase:${result.phase}`);
    });
}

// ── Pregnancy Test Calculator ─────────────────────────────────────────────────
function initPregnancyTest() {
    const form = document.getElementById('pregnancyTestForm');
    if (!form) return;
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const lastPeriod = document.getElementById('ptLastPeriod').value;
        const cycleLen   = parseInt(document.getElementById('ptCycleLen').value, 10) || 28;
        const result     = calcCycle(lastPeriod, cycleLen, 5);
        const earliest   = addDays(new Date(result.nextPeriod), 1);
        const out = document.getElementById('pregnancyTestResult');
        if (out) out.innerHTML = `<p>✅ Earliest reliable test date: <strong>${fmt(earliest)}</strong></p>`;
        await logActivity('PREGNANCY_TEST');
    });
}

// ── Pregnancy Weeks to Months ─────────────────────────────────────────────────
function initPregnancyWeeks() {
    const form = document.getElementById('pregnancyWeeksForm');
    if (!form) return;
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const weeks  = parseInt(document.getElementById('pregWeeks').value, 10);
        const months = (weeks / 4.33).toFixed(1);
        const out = document.getElementById('pregnancyWeeksResult');
        if (out) out.innerHTML = `<p>📅 ${weeks} weeks ≈ <strong>${months} months</strong></p>`;
        await logActivity('PREGNANCY_WEEKS', `${weeks}wks`);
    });
}

// ── Symptom Tracker ───────────────────────────────────────────────────────────
function initSymptomTracker() {
    const form = document.getElementById('symptomForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const user = getUser();
        if (!user) return;

        const symptomName = document.getElementById('symptomName').value.trim();
        const severity    = parseInt(document.getElementById('symptomSeverity').value, 10);

        if (!symptomName || isNaN(severity)) {
            alert('Please fill in all fields.'); return;
        }

        const data = await apiFetch('/symptoms', {
            method: 'POST',
            body: JSON.stringify({ userId: user.userId, symptomName, severity })
        });

        if (data?.success) {
            showMessage('symptomMessage', '✔ Symptom saved!', 'success');
            form.reset();
            await loadSymptomHistory(user.userId);
        } else {
            showMessage('symptomMessage', 'Failed to save symptom.', 'error');
        }
    });

    // Load history on page load
    const user = getUser();
    if (user) loadSymptomHistory(user.userId);
}

async function loadSymptomHistory(userId) {
    const container = document.getElementById('symptomHistory');
    if (!container) return;

    const history = await apiFetch(`/symptoms/${userId}`);
    if (!history || !history.length) {
        container.innerHTML = '<p>No symptom history yet.</p>';
        return;
    }

    container.innerHTML = history.map(s => `
        <div class="history-item">
            <span>${s.dateLogged} — <strong>${s.symptomName}</strong></span>
            <span>${'█'.repeat(s.severity)}${'░'.repeat(5 - s.severity)} ${s.severity}/5</span>
        </div>`).join('');
}

// ── Water Intake Tracker ──────────────────────────────────────────────────────
function initWaterTracker() {
    const form = document.getElementById('waterForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const user = getUser();
        if (!user) return;

        const glasses = parseInt(document.getElementById('glassesConsumed').value, 10);
        const goal    = parseInt(document.getElementById('waterGoal').value, 10) || 8;

        const data = await apiFetch('/water', {
            method: 'POST',
            body: JSON.stringify({ userId: user.userId, glassesConsumed: glasses, goal })
        });

        if (data?.success) {
            showMessage('waterMessage', `✔ Saved! ${glasses}/${goal} glasses logged.`, 'success');
            form.reset();
            await loadWaterHistory(user.userId);
        } else {
            showMessage('waterMessage', 'Failed to save water intake.', 'error');
        }
    });

    const user = getUser();
    if (user) loadWaterHistory(user.userId);
}

async function loadWaterHistory(userId) {
    const container = document.getElementById('waterHistory');
    if (!container) return;

    const history = await apiFetch(`/water/${userId}`);
    if (!history || !history.length) {
        container.innerHTML = '<p>No water history yet.</p>';
        return;
    }

    container.innerHTML = history.map(w => {
        const pct = Math.min(100, Math.round((w.glassesConsumed / w.goal) * 100));
        return `
            <div class="history-item">
                <span>${w.dateLogged}</span>
                <span>${w.glassesConsumed}/${w.goal} glasses</span>
                <div class="progress-bar" style="width:${pct}%;background:#9b5bb5;height:6px;border-radius:3px;"></div>
            </div>`;
    }).join('');
}

// ── Reminders ─────────────────────────────────────────────────────────────────
function initReminders() {
    const form = document.getElementById('reminderForm');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const user = getUser();
        if (!user) return;

        const reminderType = document.getElementById('reminderType').value;
        const reminderDate = document.getElementById('reminderDate').value;

        if (!reminderType || !reminderDate) {
            alert('Please fill in all fields.'); return;
        }

        const data = await apiFetch('/reminders', {
            method: 'POST',
            body: JSON.stringify({ userId: user.userId, reminderType, reminderDate })
        });

        if (data?.success) {
            showMessage('reminderMessage', '✔ Reminder set!', 'success');
            form.reset();
            await renderReminders('reminderList');
        } else {
            showMessage('reminderMessage', 'Failed to set reminder.', 'error');
        }
    });

    // Mark done buttons
    document.addEventListener('click', async (e) => {
        if (e.target.classList.contains('btn-mark-done')) {
            const user = getUser();
            if (!user) return;
            const reminderType = e.target.dataset.type;
            await apiFetch('/reminders/done', {
                method: 'PUT',
                body: JSON.stringify({ userId: user.userId, reminderType })
            });
            await renderReminders('reminderList');
            await renderReminders('homeReminders');
        }
    });

    const user = getUser();
    if (user) renderReminders('reminderList');
}

async function renderReminders(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const user = getUser();
    if (!user) return;

    const reminders = await apiFetch(`/reminders/${user.userId}`);
    if (!reminders || !reminders.length) {
        container.innerHTML = '<p style="color:#9b5bb5;">✨ No upcoming reminders.</p>';
        return;
    }

    const today = fmt(new Date());
    container.innerHTML = reminders.map((r) => {
        const d    = new Date(r.reminderDate + 'T00:00:00');
        const t    = new Date(today + 'T00:00:00');
        const days = Math.round((d - t) / 86400000);
        const when = days === 0 ? 'TODAY' : days === 1 ? 'Tomorrow' : `In ${days} days`;
        const urgent = days <= 1 ? ' urgent' : '';
        return `<div class="reminder-item${urgent}">
            <span>🔔 <strong>${r.reminderType}</strong> — ${r.reminderDate} (${when})</span>
            <span>
                <span class="badge badge-pending">${r.status}</span>
                <button class="btn-mark-done" data-type="${r.reminderType}" 
                        style="margin-left:8px;cursor:pointer;">Done</button>
            </span>
        </div>`;
    }).join('');
}

async function initHomeReminders() {
    await renderReminders('homeReminders');
}

// ── Exercise & Mood ───────────────────────────────────────────────────────────
function initPhaseCards() {
    document.querySelectorAll('[data-phase]').forEach((card) => {
        card.addEventListener('click', async () => {
            const id = card.dataset.phase;
            document.querySelectorAll('.phase-detail').forEach((d) => d.classList.remove('show'));
            const detail = document.getElementById(id);
            if (detail) detail.classList.add('show');
            const titles = {
                'phase-menstrual':  'Menstrual Phase',
                'phase-follicular': 'Follicular Phase',
                'phase-ovulation':  'Ovulation Phase',
                'phase-luteal':     'Luteal Phase',
                'phase-lifestyle':  'Lifestyle Tips'
            };
            if (titles[id]) await logActivity('EXERCISE_MOOD', titles[id]);
        });
    });
}

// ── PCOS Guide ────────────────────────────────────────────────────────────────
function initPcosGuide() {
    document.querySelectorAll('[data-pcos]').forEach((el) => {
        el.addEventListener('click', async () => {
            await logActivity('PCOS_GUIDE', el.dataset.pcos);
        });
    });
}

// ── Goodie Basket ─────────────────────────────────────────────────────────────
const BASKET_OPTIONS = {
    heavy: {
        flow: 'Heavy Flow',
        chocolates: ['Dark Chocolate', 'Snickers', 'KitKat'],
        painkillers: ['Ibuprofen', 'Mefenamic Acid'],
        pads: ['Maxi Thick Pads', 'Overnight Pads', 'Extra Long Wings Pads']
    },
    medium: {
        flow: 'Medium Flow',
        chocolates: ['Dairy Milk', 'Galaxy', 'Ferrero Rocher'],
        painkillers: ['Ibuprofen (low dose)', 'Brufen'],
        pads: ['Regular Wings Pads', 'Cotton Soft Pads']
    },
    light: {
        flow: 'Light Flow',
        chocolates: ['Milky Bar', 'Cookies n Cream', 'Kinder Bueno'],
        painkillers: ['No painkiller needed', 'Panadol'],
        pads: ['Panty Liners', 'Ultra Thin Pads']
    }
};

let selectedBasket = null;

function initGoodieBasket() {
    document.querySelectorAll('.flow-option').forEach((el) => {
        el.addEventListener('click', () => {
            document.querySelectorAll('.flow-option').forEach((x) => x.classList.remove('selected'));
            el.classList.add('selected');
            selectedBasket = BASKET_OPTIONS[el.dataset.flow];
            showBasketStep(2);
        });
    });

    const orderForm = document.getElementById('orderForm');
    if (orderForm) {
        orderForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const user = getUser();
            if (!user || !selectedBasket) return;

            const padIdx   = parseInt(document.getElementById('padChoice').value, 10) - 1;
            const pad      = selectedBasket.pads[padIdx] || selectedBasket.pads[0];
            const delivery = document.querySelector('input[name="delivery"]:checked')?.value || 'standard';
            const payment  = document.querySelector('input[name="payment"]:checked')?.value || 'cod';
            const fee      = delivery === 'express' ? 250 : 150;
            const deliveryLabel  = delivery === 'express' ? 'Express Delivery (1-2 days)' : 'Standard Delivery (3-5 days)';
            const paymentLabels  = { cod: 'Cash on Delivery', easypaisa: 'EasyPaisa', jazzcash: 'JazzCash' };

            const orderPayload = {
                userId:         user.userId,
                flowType:       selectedBasket.flow,
                padSelected:    pad,
                chocolatesList: selectedBasket.chocolates.join(', '),
                painkillerName: selectedBasket.painkillers[0],
                fullName:       document.getElementById('custName').value,
                phoneNumber:    document.getElementById('custPhone').value,
                homeAddress:    document.getElementById('custAddress').value,
                city:           document.getElementById('custCity').value,
                deliveryMethod: deliveryLabel,
                paymentMethod:  paymentLabels[payment],
                deliveryFeeRs:  fee
            };

            const data = await apiFetch('/orders', {
                method: 'POST',
                body: JSON.stringify(orderPayload)
            });

            if (data?.success) {
                document.getElementById('orderReceipt').innerHTML = `
                    <h3>📋 Order Confirmation</h3>
                    <ul>
                        <li><strong>Name:</strong> ${orderPayload.fullName}</li>
                        <li><strong>Phone:</strong> ${orderPayload.phoneNumber}</li>
                        <li><strong>City:</strong> ${orderPayload.city}</li>
                        <li><strong>Flow:</strong> ${selectedBasket.flow}</li>
                        <li><strong>Pad:</strong> ${pad}</li>
                        <li><strong>Delivery:</strong> ${deliveryLabel} — Rs ${fee}</li>
                        <li><strong>Payment:</strong> ${paymentLabels[payment]}</li>
                    </ul>
                    <p class="alert-success">✅ Order placed successfully!</p>`;
                showBasketStep(4);
            } else {
                alert('Order failed. Please try again.');
            }
        });
    }
}

function showBasketStep(n) {
    document.querySelectorAll('.basket-step').forEach((s) => {
        s.style.display = s.dataset.step == n ? 'block' : 'none';
    });
    if (n === 2 && selectedBasket) {
        document.getElementById('basketContents').innerHTML = `
            <p><strong>${selectedBasket.flow}</strong></p>
            <p>🍫 Chocolates: ${selectedBasket.chocolates.join(', ')}</p>
            <p>💊 Painkillers: ${selectedBasket.painkillers.join(', ')}</p>
            <label>📦 Pad option</label>
            <select id="padChoice" class="form-control-custom">
                ${selectedBasket.pads.map((p, i) => `<option value="${i + 1}">${p}</option>`).join('')}
            </select>
            <button type="button" class="btn-submit" style="margin-top:16px" onclick="showBasketStep(3)">Proceed to delivery →</button>`;
    }
}

window.showBasketStep = showBasketStep;

// ── Initialize Everything ─────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', async () => {
    if (document.body.dataset.page === 'auth') {
        if (getUser()) {
            window.location.href = 'home.html';
            return;
        }
        await initAuth();
        return;
    }

    if (document.body.dataset.requireAuth === 'true') {
        const user = requireAuth();
        if (!user) return;
    }

    showWelcome();
    initCycleTracker();
    initPregnancyTest();
    initPregnancyWeeks();
    initSymptomTracker();
    initWaterTracker();
    initReminders();
    initPhaseCards();
    initPcosGuide();
    initGoodieBasket();
    initHomeReminders();

    if (document.body.dataset.page === 'about') logActivity('VIEW_ABOUT');
    if (document.body.dataset.page === 'home')  logActivity('VIEW_HOME');
});