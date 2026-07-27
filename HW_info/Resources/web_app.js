const API_BASE = '';

let state = {
  view: 'machines',
  datas: [],
  changelogs: [],
  stats: {},
  selectedMac: null,
  columnVisibility: {
    '计算机名': true, '用户名': true, '操作系统': true, '型号': true,
    '序列号': true, 'CPU': true, '主板': true, '内存': true,
    '硬盘': true, '显卡': true, '显示器': false, '打印机': false,
    '网卡': false, 'IP地址': false, 'MAC地址': false, '应用程序': false,
    '姓名': false, '位置': false, '备注': false, '提交时间': false,
  },
};

async function apiGet(path) {
  const r = await fetch(API_BASE + path);
  return r.json();
}

function navigate(view, params) {
  state.view = view;
  document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
  const navMap = { 'machines': 0, 'changelogs': 1 };
  const items = document.querySelectorAll('.nav-item');
  if (navMap[view] !== undefined) items[navMap[view]]?.classList.add('active');
  render();
}

async function render() {
  const content = document.getElementById('content');
  if (state.view === 'machines') {
    await loadStats();
    await loadMachines();
    content.innerHTML = renderMachinesView();
    attachTableEvents();
  } else if (state.view === 'changelogs') {
    await loadChangeLogs();
    content.innerHTML = renderChangeLogsView();
  } else if (state.view === 'machine-detail') {
    await loadMachineDetail(state.selectedMac);
    content.innerHTML = renderMachineDetailView();
    attachTableEvents();
  }
}

async function loadStats() {
  state.stats = await apiGet('/api/stats');
}

async function loadMachines() {
  state.datas = await apiGet('/api/datas/latest');
}

async function loadChangeLogs(mac) {
  const path = mac ? `/api/changelogs?mac=${encodeURIComponent(mac)}` : '/api/changelogs?limit=100';
  state.changelogs = await apiGet(path);
}

async function loadMachineDetail(mac) {
  const d = await apiGet(`/api/datas/history?mac=${encodeURIComponent(mac)}`);
  state.datas = d.records || [];
}

function getColumnLabel(name) {
  const labels = {
    '计算机名': 'Computer', '用户名': 'User', '操作系统': 'OS', '系统安装日期': 'Install Date',
    '型号': 'Model', 'BIOS日期': 'BIOS Date', '序列号': 'Serial', 'CPU': 'CPU',
    '主板': 'Motherboard', '内存': 'Memory', '硬盘': 'Disk', '显卡': 'GPU',
    '显示器': 'Monitor', '打印机': 'Printer', '网卡': 'NIC', 'IP地址': 'IP',
    'MAC地址': 'MAC', '应用程序': 'Apps', '姓名': 'Name', '位置': 'Location',
    '备注': 'Note', '提交时间': 'Submitted',
  };
  return labels[name] || name;
}

function renderMachinesView() {
  const s = state.stats;
  const visibleCols = Object.entries(state.columnVisibility).filter(([,v]) => v).map(([k]) => k);
  const allCols = Object.keys(state.columnVisibility);

  return `
    <div class="stats-row">
      <div class="stat-card">
        <div class="label">总记录数</div>
        <div class="value">${s.totalReports || 0}</div>
      </div>
      <div class="stat-card">
        <div class="label">在线机器</div>
        <div class="value">${s.totalMachines || 0}</div>
      </div>
      <div class="stat-card">
        <div class="label">今日新增</div>
        <div class="value">${s.todayNew || 0}</div>
      </div>
    </div>

    <div class="toolbar">
      <div class="toolbar-left">
        <strong style="font-size:16px;">机器列表</strong>
        <span style="color:#64748b;font-size:13px;">${state.datas.length} 台</span>
      </div>
      <div class="toolbar-right">
        <div class="column-toggle">
          <button class="btn btn-sm" onclick="toggleColumnMenu()">☰ 列显示</button>
          <div class="column-toggle-menu" id="columnMenu">
            ${allCols.map(col => `
              <label class="column-toggle-item">
                <input type="checkbox" ${state.columnVisibility[col] ? 'checked' : ''} 
                       onchange="toggleColumn('${col}', this.checked)">
                <span>${col}</span>
              </label>
            `).join('')}
          </div>
        </div>
        <a href="/api/export/csv" class="btn btn-sm btn-primary" target="_blank">⬇ 导出 CSV</a>
      </div>
    </div>

    <div class="table-container">
      <table>
        <thead>
          <tr>
            <th>#</th>
            ${visibleCols.map(col => `<th>${getColumnLabel(col)}</th>`).join('')}
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          ${state.datas.map((d, i) => `
            <tr>
              <td>${i + 1}</td>
              ${visibleCols.map(col => `<td title="${(d[col] || '').replace(/\|/g, ' | ')}">${(d[col] || '').replace(/\|/g, ' | ').substring(0, 50)}</td>`).join('')}
              <td>
                <button class="btn btn-sm" onclick="showDetail('${d.MAC地址}')">📋 历史</button>
              </td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>
  `;
}

function renderChangeLogsView() {
  return `
    <div class="section-title">变更记录</div>
    ${state.changelogs.length === 0 ? '<p style="color:#64748b;">暂无变更记录</p>' : ''}
    <div class="table-container">
      <table>
        <thead>
          <tr>
            <th>时间</th>
            <th>计算机名</th>
            <th>变更字段</th>
            <th>旧值</th>
            <th>新值</th>
          </tr>
        </thead>
        <tbody>
          ${state.changelogs.map(log => `
            <tr>
              <td>${log.ChangedAt || ''}</td>
              <td>${log.ComputerName || ''}</td>
              <td><span class="changelog-field">${log.FieldName}</span></td>
              <td style="color:#ef4444;">${log.OldValue || '-'}</td>
              <td style="color:#22c55e;">${log.NewValue || '-'}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>
  `;
}

function renderMachineDetailView() {
  const mac = state.selectedMac;
  const records = state.datas;
  const latest = records[0] || {};
  const allFields = Object.keys(state.columnVisibility);
  const compareFields = ['计算机名','用户名','操作系统','型号','序列号','CPU','主板','内存','硬盘','显卡','显示器'];

  return `
    <div class="machine-header">
      <a class="back-btn" onclick="navigate('machines')">← 返回列表</a>
      <span style="font-size:20px;font-weight:600;">${latest.计算机名 || mac}</span>
    </div>

    <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px;">
      <div class="stat-card">
        <div class="label">MAC 地址</div>
        <div class="value" style="font-size:16px;">${mac}</div>
      </div>
      <div class="stat-card">
        <div class="label">提交次数</div>
        <div class="value" style="font-size:16px;">${records.length}</div>
      </div>
    </div>

    <div class="section-title">当前配置</div>
    <div class="table-container" style="margin-bottom:24px;">
      <table class="history-table">
        <tbody>
          ${allFields.filter(f => f !== '提交时间').map(f => `
            <tr>
              <td style="font-weight:600;width:120px;">${f}</td>
              <td>${(latest[f] || '-').replace(/\|/g, ' | ')}</td>
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>

    <div class="section-title">提交历史</div>
    <div class="table-container" style="margin-bottom:24px;">
      <table class="history-table">
        <thead>
          <tr>
            <th>#</th>
            <th>提交时间</th>
            ${compareFields.map(f => `<th>${getColumnLabel(f)}</th>`).join('')}
          </tr>
        </thead>
        <tbody>
          ${records.map((r, i) => `
            <tr>
              <td>${i + 1}</td>
              <td>${r.提交时间 || ''}</td>
              ${compareFields.map(f => {
                const val = r[f] || '-';
                return `<td title="${val.replace(/\|/g, ' | ')}">${val.replace(/\|/g, ' | ').substring(0, 30)}</td>`;
              }).join('')}
            </tr>
          `).join('')}
        </tbody>
      </table>
    </div>

    <div class="section-title">变更记录</div>
    <div id="detailChangelogs"></div>
  `;
}

function toggleColumnMenu() {
  document.getElementById('columnMenu').classList.toggle('show');
}

function toggleColumn(name, visible) {
  state.columnVisibility[name] = visible;
  render();
}

function showDetail(mac) {
  if (!mac) { alert('该机器没有 MAC 地址'); return; }
  state.selectedMac = mac;
  navigate('machine-detail');
}

function attachTableEvents() {
  if (state.view === 'machine-detail' && state.selectedMac) {
    loadChangeLogs(state.selectedMac).then(() => {
      const el = document.getElementById('detailChangelogs');
      if (!el) return;
      el.innerHTML = state.changelogs.length === 0
        ? '<p style="color:#64748b;">无变更记录</p>'
        : `<div class="table-container"><table class="history-table"><thead><tr><th>时间</th><th>字段</th><th>旧值</th><th>新值</th></tr></thead><tbody>
          ${state.changelogs.map(log => `
            <tr>
              <td>${log.ChangedAt || ''}</td>
              <td><span class="changelog-field">${log.FieldName}</span></td>
              <td style="color:#ef4444;max-width:200px;overflow:hidden;text-overflow:ellipsis;">${log.OldValue || '-'}</td>
              <td style="color:#22c55e;max-width:200px;overflow:hidden;text-overflow:ellipsis;">${log.NewValue || '-'}</td>
            </tr>
          `).join('')}</tbody></table></div>`;
    });
  }
}

document.addEventListener('click', function(e) {
  const menu = document.getElementById('columnMenu');
  if (menu && !e.target.closest('.column-toggle')) {
    menu.classList.remove('show');
  }
});

navigate('machines');
