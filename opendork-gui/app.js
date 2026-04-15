const stateKey = 'opendork.gui.state.v1';

const defaultState = {
  spendUsd: 0.23,
  budgetUsd: 5,
  runsQueued: 4,
  successRate: 92,
  models: [
    { modelName: 'gpt-4o', providerClient: 'openai-compatible', inputCostPer1K: 0.005, outputCostPer1K: 0.015, enabled: true },
    { modelName: 'gpt-4o-mini', providerClient: 'local-fallback', inputCostPer1K: 0, outputCostPer1K: 0, enabled: true }
  ],
  runs: [
    { runId: 'run-1042', profile: 'interactive', model: 'gpt-4o', status: 'gold', score: 7 },
    { runId: 'run-1041', profile: 'batch', model: 'gpt-4o-mini', status: 'validated', score: 4 }
  ],
  artifacts: { raw: 12, validated: 8, rejected: 3, gold: 5, reports: 4 }
};

const loadState = () => JSON.parse(localStorage.getItem(stateKey) || JSON.stringify(defaultState));
const saveState = (state) => localStorage.setItem(stateKey, JSON.stringify(state));

let state = loadState();

const el = (id) => document.getElementById(id);

function renderStats() {
  const spendPct = ((state.spendUsd / state.budgetUsd) * 100).toFixed(1);
  const cards = [
    ['Spend', `$${state.spendUsd.toFixed(2)} / $${state.budgetUsd.toFixed(2)}`, `${spendPct}% budget used`],
    ['Runs Queued', `${state.runsQueued}`, 'Interactive + overnight'],
    ['Success Rate', `${state.successRate}%`, 'gold + validated'],
    ['Models', `${state.models.length}`, 'active catalog entries']
  ];

  el('stats-grid').innerHTML = cards
    .map(
      ([title, value, subtitle]) => `
      <article class="rounded-2xl border border-slate-800 bg-slate-900/70 p-4">
        <p class="text-xs uppercase tracking-wide text-slate-400">${title}</p>
        <p class="mt-2 text-2xl font-semibold">${value}</p>
        <p class="mt-1 text-xs text-slate-400">${subtitle}</p>
      </article>
    `
    )
    .join('');
}

function renderModels() {
  el('model').innerHTML = state.models
    .map((m) => `<option value="${m.modelName}">${m.modelName}</option>`)
    .join('');

  el('models-table').innerHTML = state.models
    .map(
      (m) => `
      <tr>
        <td class="py-3 pr-4 font-medium">${m.modelName}</td>
        <td class="py-3 pr-4 text-slate-300">${m.providerClient}</td>
        <td class="py-3 pr-4 text-slate-300">${m.inputCostPer1K}</td>
        <td class="py-3 pr-4 text-slate-300">${m.outputCostPer1K}</td>
        <td class="py-3 pr-4">${m.enabled ? '<span class="rounded bg-emerald-500/20 px-2 py-1 text-emerald-300">enabled</span>' : '<span class="rounded bg-rose-500/20 px-2 py-1 text-rose-300">disabled</span>'}</td>
        <td class="py-3">
          <button data-toggle="${m.modelName}" class="rounded border border-slate-700 px-2 py-1 text-xs hover:border-brand-500">toggle</button>
        </td>
      </tr>
    `
    )
    .join('');

  document.querySelectorAll('[data-toggle]').forEach((btn) => {
    btn.addEventListener('click', () => {
      const modelName = btn.dataset.toggle;
      state.models = state.models.map((m) =>
        m.modelName === modelName ? { ...m, enabled: !m.enabled } : m
      );
      saveState(state);
      renderAll();
    });
  });
}

function renderRuns() {
  el('runs-list').innerHTML = state.runs
    .slice(0, 8)
    .map(
      (run) => `
      <li class="rounded-xl border border-slate-800 bg-slate-950/70 p-3 text-sm">
        <div class="flex items-center justify-between">
          <span class="font-medium">${run.runId}</span>
          <span class="text-xs ${run.status === 'gold' ? 'text-amber-300' : 'text-emerald-300'}">${run.status}</span>
        </div>
        <p class="mt-1 text-xs text-slate-400">${run.profile} • ${run.model} • score ${run.score}</p>
      </li>
    `
    )
    .join('');
}

function renderArtifacts() {
  el('raw-count').textContent = state.artifacts.raw;
  el('validated-count').textContent = state.artifacts.validated;
  el('rejected-count').textContent = state.artifacts.rejected;
  el('gold-count').textContent = state.artifacts.gold;
  el('report-count').textContent = state.artifacts.reports;
}

function bindForm() {
  const prompt = el('prompt');
  const profile = el('profile');
  const model = el('model');
  const preview = el('cli-preview');

  const updatePreview = () => {
    preview.textContent = `opendork-cli run "${prompt.value || '...'}" ${profile.value} ${model.value || 'gpt-4o'}`;
  };

  [prompt, profile, model].forEach((node) => node.addEventListener('input', updatePreview));
  updatePreview();

  el('run-form').addEventListener('submit', (event) => {
    event.preventDefault();

    const runId = `run-${Math.floor(Math.random() * 9000 + 1000)}`;
    const status = Math.random() > 0.4 ? 'gold' : 'validated';
    const score = status === 'gold' ? 7 : 4;

    state.runs.unshift({ runId, profile: profile.value, model: model.value, status, score });
    state.runsQueued += 1;
    state.spendUsd += status === 'gold' ? 0.04 : 0.01;
    state.artifacts.raw += 1;
    state.artifacts[status] += 1;
    if (Math.random() > 0.5) state.artifacts.reports += 1;

    saveState(state);
    renderAll();
    prompt.value = '';
    updatePreview();
  });
}

function bindModelCreate() {
  el('add-model-btn').addEventListener('click', () => {
    const name = prompt('Model name? (e.g. my-model)');
    if (!name) return;

    const provider = prompt('Provider client?', 'openai-compatible') || 'openai-compatible';
    const inCost = Number(prompt('Input cost per 1K?', '0.002') || '0.002');
    const outCost = Number(prompt('Output cost per 1K?', '0.006') || '0.006');

    state.models.push({
      modelName: name,
      providerClient: provider,
      inputCostPer1K: inCost,
      outputCostPer1K: outCost,
      enabled: true
    });

    saveState(state);
    renderAll();
  });
}

function renderAll() {
  renderStats();
  renderModels();
  renderRuns();
  renderArtifacts();
}

renderAll();
bindForm();
bindModelCreate();
