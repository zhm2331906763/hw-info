const API_BASE = '';

let state = {
  view: 'machines',
  datas: [],
  changelogs: [],
  stats: {},
  selectedMac: null,
  detailLogs: [],
  searchTerm: '',
  pageSize: 20,
  currentPage: 1,
  showHistory: false,
  columnVisibility: {
    '计算机名': true, '用户名': true, '操作系统': false, '型号': true,
    '序列号': false, 'CPU数量': true, 'CPU': true, '主板': true,
    '内存数量': true, '内存': true, '硬盘数量': true, '硬盘': true,
    '显卡': true, '显示器数量': true, '显示器': true,
    '系统安装日期': false, 'BIOS日期': false, '打印机': false,
    '网卡': false, 'IP地址': false, 'MAC地址': false, '应用程序': false,
    '姓名': false, '位置': false, '备注': false, '提交时间': false,
  },
};

const allCols = [
  '计算机名','用户名','操作系统','系统安装日期','型号','BIOS日期','序列号',
  'CPU数量','CPU','主板','内存数量','内存','硬盘数量','硬盘',
  '显卡','显示器数量','显示器','打印机','网卡','IP地址','MAC地址',
  '应用程序','姓名','位置','备注','提交时间',
];
const countSrc = { 'CPU': 'CPU数量', '内存': '内存数量', '硬盘': '硬盘数量', '显示器': '显示器数量' };

function fmtTime(t) {
  if (!t) return '-';
  function p(n) { return n < 10 ? '0' + n : '' + n; }
  //处理 /Date(ms)/ 格式
  var m = /\/Date\((\d+)\)\//.exec(t);
  if (m) {
    var d = new Date(parseInt(m[1]));
    return d.getFullYear() + '-' + (d.getMonth()+1) + '-' + d.getDate() + '-' + p(d.getHours()) + ':' + p(d.getMinutes());
  }
  //处理 ISO 格式
  var d2 = new Date(t.replace(' ','T'));
  if (!isNaN(d2)) {
    return d2.getFullYear() + '-' + (d2.getMonth()+1) + '-' + d2.getDate() + '-' + p(d2.getHours()) + ':' + p(d2.getMinutes());
  }
  return t.substring(0,16);
}

function countItems(v) {
  if (!v) return 0;
  return v.split('|').filter(function(s) { return s.trim().length > 0; }).length;
}

function apiGet(path) {
  return fetch(API_BASE + path).then(function(r) { return r.json(); });
}

function navigate(view) {
  state.view = view;
  state.currentPage = 1;
  state.searchTerm = '';
  state.showHistory = false;
  var items = document.querySelectorAll('.nav-item');
  items.forEach(function(el) { el.classList.remove('active'); });
  var idx = { 'machines': 0, 'changelogs': 1 }[view];
  if (idx !== undefined) items[idx].classList.add('active');
  render();
}

function render() {
  var container = document.getElementById('content');
  if (!container) return;
  try {
    if (state.view === 'machines') {
      loadStats().then(function() {
        return loadMachines();
      }).then(function() {
        container.innerHTML = buildMachinesView();
      }).catch(function(err) {
        container.innerHTML = '<p style="color:red;padding:40px;">加载失败: ' + err.message + '</p>';
      });
    } else if (state.view === 'changelogs') {
      loadChangeLogs().then(function() {
        container.innerHTML = buildChangeLogsView();
      }).catch(function(err) {
        container.innerHTML = '<p style="color:red;padding:40px;">加载失败: ' + err.message + '</p>';
      });
    } else if (state.view === 'machine-detail') {
      container.innerHTML = '<div style="text-align:center;padding:40px;color:var(--text-muted);">加载中...</div>';
      Promise.all([
        apiGet('/api/datas/history?mac=' + state.selectedMac),
        apiGet('/api/changelogs?mac=' + state.selectedMac)
      ]).then(function(results) {
        state.datas = results[0].records || [];
        var logs = results[1];
        state.detailLogs = Array.isArray(logs) ? logs : [];
        container.innerHTML = buildDetailView();
      }).catch(function(err) {
        container.innerHTML = '<p style="color:red;padding:40px;">请求失败: ' + err.message + '<br>MAC=' + state.selectedMac + '</p>';
      });
    }
  } catch (err) {
    container.innerHTML = '<p style="color:red;padding:40px;">脚本错误: ' + err.message + '<br>' + err.stack + '</p>';
  }
}

function loadStats() {
  return apiGet('/api/stats').then(function(s) { state.stats = s; });
}

function loadMachines() {
  return apiGet('/api/datas/latest').then(function(d) { state.datas = d; });
}

function loadChangeLogs(mac) {
  var path = mac ? '/api/changelogs?mac=' + mac : '/api/changelogs?limit=100';
  return apiGet(path).then(function(l) { state.changelogs = l; });
}

function getFiltered() {
  if (!state.searchTerm) return state.datas;
  var t = state.searchTerm.toLowerCase();
  return state.datas.filter(function(d) {
    return (d.计算机名 || '').toLowerCase().includes(t) || (d.用户名 || '').toLowerCase().includes(t);
  });
}

function getPageData(list) {
  var start = (state.currentPage - 1) * state.pageSize;
  return list.slice(start, start + state.pageSize);
}

function totalPages(list) {
  return Math.max(1, Math.ceil(list.length / state.pageSize));
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
  var inp = document.getElementById('searchInput');
  state.searchTerm = inp ? inp.value : '';
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
  var list = getFiltered();
  var tp = totalPages(list);
  state.currentPage = Math.max(1, Math.min(tp, state.currentPage + delta));
  render();
}

function toggleHistory() {
  state.showHistory = !state.showHistory;
  render();
}

function cellValue(col, val) {
  if (!val) return '-';
  return val.replace(/\|/g, ' | ').substring(0, 60);
}

function cellHtml(col, val) {
  if (!val) return '-';
  return val.replace(/\|/g, '<br>');
}

// --- Builders ---

function buildMachinesView() {
  var s = state.stats;
  var visible = allCols.filter(function(c) { return state.columnVisibility[c]; });
  var filtered = getFiltered();
  var page = getPageData(filtered);
  var tp = totalPages(filtered);
  var startRow = (state.currentPage - 1) * state.pageSize + 1;

  var html = '';
  html += '<div class="stats-row">';
  html += '<div class="stat-card"><div class="label">总记录数</div><div class="value">' + (s.totalReports || 0) + '</div></div>';
  html += '<div class="stat-card"><div class="label">已收集机器数</div><div class="value">' + (s.totalMachines || 0) + '</div></div>';
  html += '<div class="stat-card"><div class="label">今日新增</div><div class="value">' + (s.todayNew || 0) + '</div></div>';
  html += '</div>';

  html += '<div class="table-title">硬件资产信息表</div>';

  html += '<div class="toolbar"><div class="toolbar-left">';
  html += '<input id="searchInput" type="text" class="search-input" placeholder="计算机名 / 用户名" onkeydown="onSearchKey(event)">';
  html += '<button class="btn btn-sm btn-primary" onclick="doSearch()">搜索</button>';
  html += '<span style="font-size:13px;color:var(--text-muted);">' + filtered.length + ' 台匹配</span>';
  html += '</div><div class="toolbar-right">';

  html += '<div class="column-toggle">';
  html += '<button class="btn btn-sm" onclick="document.getElementById(\'columnMenu\').classList.toggle(\'show\')">☰ 列显示</button>';
  html += '<div class="column-toggle-menu" id="columnMenu">';
  allCols.forEach(function(c) {
    html += '<label class="column-toggle-item"><input type="checkbox"' + (state.columnVisibility[c] ? ' checked' : '') + ' onchange="toggleColumn(\'' + c + '\',this.checked)"><span>' + c + '</span></label>';
  });
  html += '</div></div>';

  html += '<a href="/api/export/csv" class="btn btn-sm" target="_blank">⬇ CSV</a>';
  html += '<a href="/api/export/xlsx" class="btn btn-sm btn-primary" target="_blank">⬇ XLSX</a>';
  html += '</div></div>';

  html += '<div class="table-container"><table><thead><tr><th>#</th>';
  visible.forEach(function(c) { html += '<th>' + c + '</th>'; });
  html += '</tr></thead><tbody>';

  page.forEach(function(d, i) {
    var mac = (d.MAC地址 || '').replace(/'/g, '\\\'');
    html += '<tr onclick="showDetail(\'' + mac + '\')" style="cursor:pointer;">';
    html += '<td>' + (startRow + i) + '</td>';
    visible.forEach(function(c) {
      if (countSrc[c]) {
        html += '<td title="' + cellValue(c, d[c]) + '">' + cellValue(c, d[c]) + '</td>';
        return;
      }
      var srcKey = Object.keys(countSrc).filter(function(k) { return countSrc[k] === c; })[0];
      if (srcKey) {
        html += '<td style="text-align:center;font-weight:600;">' + countItems(d[srcKey]) + '</td>';
        return;
      }
      if (c === '提交时间') {
        html += '<td>' + fmtTime(d[c]) + '</td>';
        return;
      }
      html += '<td title="' + cellValue(c, d[c]) + '">' + cellValue(c, d[c]) + '</td>';
    });
    html += '</tr>';
  });

  html += '</tbody></table></div>';

  html += '<div class="pagination">';
  html += '<span>每页</span>';
  html += '<select class="page-size-select" onchange="setPageSize(parseInt(this.value))">';
  html += '<option value="20"' + (state.pageSize === 20 ? ' selected' : '') + '>20</option>';
  html += '<option value="50"' + (state.pageSize === 50 ? ' selected' : '') + '>50</option>';
  html += '<option value="100"' + (state.pageSize === 100 ? ' selected' : '') + '>100</option>';
  html += '</select><span>条</span>';
  html += '<span style="margin:0 12px;color:var(--text-muted);">第 ' + state.currentPage + '/' + tp + ' 页</span>';
  html += '<button class="btn btn-sm" onclick="goPage(-1)" ' + (state.currentPage <= 1 ? 'disabled' : '') + '>◀ 上一页</button>';
  html += '<button class="btn btn-sm" onclick="goPage(1)" ' + (state.currentPage >= tp ? 'disabled' : '') + '>下一页 ▶</button>';
  html += '</div>';

  return html;
}

function buildChangeLogsView() {
  var html = '<div class="section-title">变更记录</div>';
  if (state.changelogs.length === 0) {
    html += '<p style="color:#64748b;">暂无变更记录</p>';
  } else {
    html += '<div class="table-container"><table><thead><tr><th>时间</th><th>计算机名</th><th>变更字段</th><th>旧值</th><th>新值</th></tr></thead><tbody>';
    state.changelogs.forEach(function(l) {
      html += '<tr>';
      html += '<td>' + fmtTime(l.ChangedAt) + '</td>';
      html += '<td>' + (l.ComputerName || '') + '</td>';
      html += '<td><span class="changelog-field">' + l.FieldName + '</span></td>';
      html += '<td style="color:#ef4444;">' + cellHtml(null, l.OldValue) + '</td>';
      html += '<td style="color:#22c55e;">' + cellHtml(null, l.NewValue) + '</td>';
      html += '</tr>';
    });
    html += '</tbody></table></div>';
  }
  return html;
}

function buildDetailView() {
  var mac = state.selectedMac;
  var records = state.datas;
  var latest = records[0] || {};
  var compare = ['计算机名','用户名','操作系统','型号','序列号','CPU','主板','内存','硬盘','显卡','显示器'];

  var html = '';
  html += '<div class="machine-header">';
  html += '<a class="back-btn" href="javascript:navigate(\'machines\')">← 返回列表</a>';
  html += '<span style="font-size:20px;font-weight:600;">' + (latest.计算机名 || mac) + '</span>';
  html += '</div>';

  html += '<div style="display:grid;grid-template-columns:1fr 1fr;gap:16px;margin-bottom:24px;">';
  html += '<div class="stat-card"><div class="label">MAC 地址</div><div class="value" style="font-size:16px;">' + mac + '</div></div>';
  html += '<div class="stat-card"><div class="label">提交次数</div><div class="value" style="font-size:16px;">' + records.length + '</div></div>';
  html += '</div>';

  html += '<div class="section-title">当前配置</div>';
  html += '<div class="table-container" style="margin-bottom:24px;"><table class="history-table"><tbody>';
  allCols.forEach(function(c) {
    if (c === '提交时间') return;
    var srcKey = Object.keys(countSrc).filter(function(k) { return countSrc[k] === c; })[0];
    if (srcKey) {
      html += '<tr><td style="font-weight:600;width:140px;">' + c + '</td><td style="text-align:center;font-weight:600;">' + countItems(latest[srcKey]) + '</td></tr>';
    } else {
      html += '<tr><td style="font-weight:600;width:140px;">' + c + '</td><td>' + cellHtml(c, latest[c]) + '</td></tr>';
    }
  });
  html += '</tbody></table></div>';

  // 提交历史（默认折叠）
  html += '<div class="section-title" style="cursor:pointer;" onclick="toggleHistory()">';
  html += '<span>' + (state.showHistory ? '▼' : '▶') + ' 提交历史</span>';
  html += '<span style="font-size:13px;color:var(--text-muted);margin-left:8px;">共 ' + records.length + ' 条</span>';
  html += '</div>';

  if (state.showHistory) {
    html += '<div class="table-container" style="margin-bottom:24px;"><table class="history-table">';
    html += '<thead><tr><th>#</th><th>提交时间</th>';
    compare.forEach(function(f) { html += '<th>' + f + '</th>'; });
    html += '</tr></thead><tbody>';
    records.forEach(function(r, i) {
      html += '<tr><td>' + (i + 1) + '</td><td>' + fmtTime(r.提交时间) + '</td>';
      compare.forEach(function(f) {
        html += '<td>' + cellHtml(f, r[f]) + '</td>';
      });
      html += '</tr>';
    });
    html += '</tbody></table></div>';
  } else {
    html += '<div style="background:var(--card-bg);border:1px solid var(--border);border-radius:12px;padding:16px;margin-bottom:24px;text-align:center;color:var(--text-muted);cursor:pointer;" onclick="toggleHistory()">点击展开提交历史</div>';
  }

  // 变更记录
  html += '<div class="section-title">变更记录</div>';
  if (state.detailLogs.length === 0) {
    html += '<p style="color:#64748b;">无变更记录</p>';
  } else {
    html += '<div class="table-container"><table class="history-table"><thead><tr><th>时间</th><th>字段</th><th>旧值</th><th>新值</th></tr></thead><tbody>';
    state.detailLogs.forEach(function(l) {
      html += '<tr>';
      html += '<td>' + fmtTime(l.ChangedAt) + '</td>';
      html += '<td><span class="changelog-field">' + l.FieldName + '</span></td>';
      html += '<td style="color:#ef4444;">' + cellHtml(null, l.OldValue) + '</td>';
      html += '<td style="color:#22c55e;">' + cellHtml(null, l.NewValue) + '</td>';
      html += '</tr>';
    });
    html += '</tbody></table></div>';
  }

  return html;
}

// --- Init ---
window.onerror = function(msg, url, line, col, err) {
  var el = document.getElementById('content');
  if (el) el.innerHTML = '<p style="color:red;padding:40px;">JS错误: ' + msg + ' (行' + line + ':' + col + ')</p>';
  return true;
};

document.addEventListener('click', function(e) {
  var m = document.getElementById('columnMenu');
  if (m && !e.target.closest('.column-toggle')) m.classList.remove('show');
});

navigate('machines');
