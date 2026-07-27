const API_BASE = '';

let state = {
  view: 'machines',
  datas: [],
  changelogs: [],
  stats: {},
  selectedMac: null,
  searchTerm: '',
  pageSize: 20,
  currentPage: 1,
  columnVisibility: {
    '计算机名': true, '用户名': true, '操作系统': false,'型号': true,
    '序列号': false,'CPU数量': true, 'CPU': true, '主板': true,
    '内存数量': true, '内存': true, '硬盘数量': true, '硬盘': true,
    '显卡': true, '显示器数量': true, '显示器': true,
    '系统安装日期': false,'BIOS日期': false,'打印机': false,
    '网卡': false,'IP地址': false,'MAC地址': false,'应用程序': false,
    '姓名': false,'位置': false,'备注': false,'提交时间': false,
  },
};

const allCols = [
  '计算机名','用户名','操作系统','系统安装日期','型号','BIOS日期','序列号',
  'CPU数量','CPU','主板','内存数量','内存','硬盘数量','硬盘',
  '显卡','显示器数量','显示器','打印机','网卡','IP地址','MAC地址',
  '应用程序','姓名','位置','备注','提交时间',
];

const countSrc = { 'CPU': 'CPU数量', '内存': '内存数量', '硬盘': '硬盘数量', '显示器': '显示器数量' };

function countOf(val) {
  if (!val) return 0;
  return val.split('|').filter(s => s.trim().length > 0).length;
}

async function apiGet(path) {
  const r = await fetch(API_BASE + path);
  return r.json();
}

function navigate(view) {
  state.view = view;
  state.currentPage = 1;
  state.searchTerm = '';
  document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
  const m = { 'machines': 0, 'changelogs': 1 };
  if (m[view] !== undefined) document.querySelectorAll('.nav-item')[m[view]]?.classList.add('active');
  render();
}

async function render() {
  const el = document.getElementById('content');
  if (state.view === 'machines') {
    await loadStats(); await loadMachines();
    el.innerHTML = renderMachinesView();
  } else if (state.view === 'changelogs') {
    await loadChangeLogs();
    el.innerHTML = renderChangeLogsView();
  } else if (state.view === 'machine-detail') {
    await loadMachineDetail(state.selectedMac);
    el.innerHTML = renderMachineDetailView();
    loadDetailChangeLogs();
  }
}

async function loadStats() { state.stats = await apiGet('/api/stats'); }
async function loadMachines() { state.datas = await apiGet('/api/datas/latest'); }
async function loadChangeLogs(mac) {
  state.changelogs = await apiGet(mac ? `/api/changelogs?mac=${encodeURIComponent(mac)}` : '/api/changelogs?limit=100');
}
async function loadMachineDetail(mac) {
  const d = await apiGet(`/api/datas/history?mac=${encodeURIComponent(mac)}`);
  state.datas = d.records || [];
}

function getFiltered() {
  let list = state.datas;
  if (state.searchTerm) {
    const t = state.searchTerm.toLowerCase();
    list = list.filter(d => (d.计算机名||'').toLowerCase().includes(t) || (d.用户名||'').toLowerCase().includes(t));
  }
  return list;
}

function getPage(filtered) {
  const start = (state.currentPage - 1) * state.pageSize;
  return filtered.slice(start, start + state.pageSize);
}

function totalPages(filtered) {
  return Math.max(1, Math.ceil(filtered.length / state.pageSize));
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

function doSearch() {
  var input = document.getElementById('searchInput');
  state.searchTerm = input ? input.value : '';
  state.currentPage = 1;
  render();
}
function onSearchKey(e) {
  if (e.key === 'Enter') doSearch();
}

function setPageSize(size) {
  state.pageSize = size;
  state.currentPage = 1;
  render();
}

function goPage(delta) {
  const filtered = getFiltered();
  const tp = totalPages(filtered);
  state.currentPage = Math.max(1, Math.min(tp, state.currentPage + delta));
  render();
}

function renderMachinesView() {
  const s = state.stats;
  const visible = allCols.filter(c => state.columnVisibility[c]);
  const filtered = getFiltered();
  const page = getPage(filtered);
  const tp = totalPages(filtered);
  const startRow = (state.currentPage - 1) * state.pageSize + 1;

  return `
    <div class="stats-row">
      <div class="stat-card"><div class="label">总记录数</div><div class="value">${s.totalReports||0}</div></div>
      <div class="stat-card"><div class="label">已收集机器数</div><div class="value">${s.totalMachines||0}</div></div>
      <div class="stat-card"><div class="label">今日新增</div><div class="value">${s.todayNew||0}</div></div>
    </div>
    <div class="table-title">硬件资产信息表</div>
    <div class="toolbar">
      <div class="toolbar-left">
        <input id="searchInput" type="text" class="search-input" placeholder="计算机名 / 用户名" onkeydown="onSearchKey(event)">
        <button class="btn btn-sm btn-primary" onclick="doSearch()">搜索</button>
        <span style="font-size:13px;color:var(--text-muted);">${filtered.length} 台匹配</span>
      </div>
      <div class="toolbar-right">
        <div class="column-toggle">
          <button class="btn btn-sm" onclick="document.getElementById('columnMenu').classList.toggle('show')">☰ 列显示</button>
          <div class="column-toggle-menu" id="columnMenu">
            ${allCols.map(c => `<label class="column-toggle-item">
              <input type="checkbox" ${state.columnVisibility[c]?'checked':''} onchange="toggleColumn('${c}',this.checked)">
              <span>${c}</span>
            </label>`).join('')}
          </div>
        </div>
        <a href="/api/export/csv" class="btn btn-sm" target="_blank">⬇ CSV</a>
        <a href="/api/export/xlsx" class="btn btn-sm btn-primary" target="_blank">⬇ XLSX</a>
      </div>
    </div>
    <div class="table-container">
      <table>
        <thead><tr><th>#</th>${visible.map(c => `<th>${c}</th>`).join('')}</tr></thead>
        <tbody>
          ${page.map((d, i) => {
            const mac = d.MAC地址 || '';
            return `<tr onclick="showDetail('${mac}')" style="cursor:pointer;">
              <td>${startRow + i}</td>
              ${visible.map(col => {
                if (countSrc[col]) {
                  return `<td title="${(d[col]||'').replace(/\|/g,' | ')}">${(d[col]||'-').replace(/\|/g,' | ').substring(0,60)}</td>`;
                }
                const srcDataCol = Object.entries(countSrc).find(([,v]) => v === col)?.[0];
                if (srcDataCol) {
                  return `<td style="text-align:center;font-weight:600;">${countOf(d[srcDataCol])}</td>`;
                }
                return `<td title="${(d[col]||'').replace(/\|/g,' | ')}">${(d[col]||'-').replace(/\|/g,' | ').substring(0,60)}</td>`;
              }).join('')}
            </tr>`;
          }).join('')}
        </tbody>
      </table>
    </div>
    <div class="pagination">
      <span>每页</span>
      <select class="page-size-select" onchange="setPageSize(parseInt(this.value))">
        <option value="20" ${state.pageSize===20?'selected':''}>20</option>
        <option value="50" ${state.pageSize===50?'selected':''}>50</option>
        <option value="100" ${state.pageSize===100?'selected':''}>100</option>
      </select>
      <span>条</span>
      <span style="margin:0 12px;color:var(--text-muted);">第 ${state.currentPage}/${tp} 页</span>
      <button class="btn btn-sm" onclick="goPage(-1)" ${state.currentPage<=1?'disabled':''}>◀ 上一页</button>
      <button class="btn btn-sm" onclick="goPage(1)" ${state.currentPage>=tp?'disabled':''}>下一页 ▶</button>
    </div>
  `;
}

function renderChangeLogsView() {
  return `
    <div class="section-title">变更记录</div>
    ${state.changelogs.length===0?'<p style="color:#64748b;">暂无变更记录</p>':''}
    <div class="table-container"><table>
      <thead><tr><th>时间</th><th>计算机名</th><th>变更字段</th><th>旧值</th><th>新值</th></tr></thead>
      <tbody>${state.changelogs.map(log => `
        <tr>
          <td>${log.ChangedAt||''}</td>
          <td>${log.ComputerName||''}</td>
          <td><span class="changelog-field">${log.FieldName}</span></td>
          <td style="color:#ef4444;">${(log.OldValue||'-').replace(/\|/g,'<br>')}</td>
          <td style="color:#22c55e;">${(log.NewValue||'-').replace(/\|/g,'<br>')}</td>
        </tr>`).join('')}</tbody>
    </table></div>`;
}

function renderMachineDetailView() {
  const mac = state.selectedMac;
  const records = state.datas;
  const latest = records[0] || {};
  const compare = ['计算机名','用户名','操作系统','型号','序列号','CPU','主板','内存','硬盘','显卡','显示器'];

  return `
    <div class="machine-header">
      <a class="back-btn" onclick="navigate('machines')">← 返回列表</a>
      <span style="font-size:20px;font-weight:600;">${latest.计算机名||mac}</span>
    </div>
    <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px;">
      <div class="stat-card"><div class="label">MAC 地址</div><div class="value" style="font-size:16px;">${mac}</div></div>
      <div class="stat-card"><div class="label">提交次数</div><div class="value" style="font-size:16px;">${records.length}</div></div>
    </div>
    <div class="section-title">当前配置</div>
    <div class="table-container" style="margin-bottom:24px;"><table class="history-table"><tbody>
      ${allCols.filter(c=>c!=='提交时间').map(c => {
        const srcDataCol = Object.entries(countSrc).find(([,v]) => v === c)?.[0];
        if (srcDataCol) {
          return `<tr><td style="font-weight:600;width:140px;">${c}</td><td style="text-align:center;font-weight:600;">${countOf(latest[srcDataCol])}</td></tr>`;
        }
        if (countSrc[c]) {
          return `<tr><td style="font-weight:600;width:140px;">${c}</td><td>${(latest[c]||'-').replace(/\|/g,'<br>')}</td></tr>`;
        }
        return `<tr><td style="font-weight:600;width:140px;">${c}</td><td>${(latest[c]||'-').replace(/\|/g,'<br>')}</td></tr>`;
      }).join('')}
    </tbody></table></div>
    <div class="section-title">提交历史</div>
    <div class="table-container" style="margin-bottom:24px;"><table class="history-table">
      <thead><tr><th>#</th><th>提交时间</th>${compare.map(f=>`<th>${f}</th>`).join('')}</tr></thead>
      <tbody>${records.map((r,i)=>`
        <tr><td>${i+1}</td><td>${r.提交时间||''}</td>
        ${compare.map(f=>`<td>${(r[f]||'-').replace(/\|/g,'<br>')}</td>`).join('')}</tr>`).join('')}
    </tbody></table></div>
    <div class="section-title">变更记录</div>
    <div id="detailChangelogs"></div>`;
}

async function loadDetailChangeLogs() {
  if (!state.selectedMac) return;
  const logs = await apiGet(`/api/changelogs?mac=${encodeURIComponent(state.selectedMac)}`);
  const el = document.getElementById('detailChangelogs');
  if (!el) return;
  el.innerHTML = logs.length===0?'<p style="color:#64748b;">无变更记录</p>'
    : `<div class="table-container"><table class="history-table"><thead><tr><th>时间</th><th>字段</th><th>旧值</th><th>新值</th></tr></thead><tbody>
      ${logs.map(l=>`<tr><td>${l.ChangedAt||''}</td><td><span class="changelog-field">${l.FieldName}</span></td>
        <td style="color:#ef4444;max-width:200px;">${(l.OldValue||'-').replace(/\|/g,'<br>')}</td>
        <td style="color:#22c55e;max-width:200px;">${(l.NewValue||'-').replace(/\|/g,'<br>')}</td></tr>`).join('')}
    </tbody></table></div>`;
}

document.addEventListener('click', function(e) {
  const m = document.getElementById('columnMenu');
  if (m && !e.target.closest('.column-toggle')) m.classList.remove('show');
});

navigate('machines');
