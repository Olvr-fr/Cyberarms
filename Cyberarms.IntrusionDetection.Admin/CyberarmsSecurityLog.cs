using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Admin {
    public partial class CyberarmsSecurityLog : UserControl {

        public event EventHandler FilterSelectionChanged;
        public const string ALL_AGENTS = "{46DD5CAD-3F50-4D69-8917-11505DB10553}";

        // ── Estado de ordenación ─────────────────────────────────────────────────
        private string _sortColumn    = "EventDate";
        private bool   _sortAscending = false;

        // ── Filtro compuesto: _baseFilter (checkboxes+combo) + _searchFilter (textbox)
        private string _baseFilter   = string.Empty;
        private string _searchFilter = string.Empty;

        private static readonly string[] SortableColumns = {
            "LogType", "LatestEntry", "NumberOfEvents", "IpAddress", "Agent"
        };

        // ── DataSet + DataView ────────────────────────────────────────────────────
        private DataSet _intrusionLog;
        public DataSet DataSetIntrusionLog {
            get {
                if (_intrusionLog == null) {
                    _intrusionLog = new DataSet();
                    _intrusionLog.Tables.Add("IntrusionLog");
                    DataTable t = _intrusionLog.Tables["IntrusionLog"];
                    t.Columns.Add("Id",             typeof(int));
                    t.Columns.Add("Action",         typeof(int));
                    t.Columns.Add("Agent",          typeof(string));
                    t.Columns.Add("LogIcon",        typeof(Image));
                    t.Columns.Add("LogType",        typeof(string));
                    t.Columns.Add("EventDate",      typeof(DateTime));
                    t.Columns.Add("IpAddress",      typeof(string));
                    t.Columns.Add("Message",        typeof(string));
                    t.Columns.Add("AgentId",        typeof(string));
                    t.Columns.Add("NumberOfEvents", typeof(int));
                    t.Columns.Add("EventDateStr",   typeof(string));  // permite LIKE sobre fechas
                }
                return _intrusionLog;
            }
            set { _intrusionLog = value; }
        }

        private DataView _intrusionLogView;
        public DataView IntrusionLogView {
            get {
                if (_intrusionLogView == null)
                    _intrusionLogView = new DataView(DataSetIntrusionLog.Tables["IntrusionLog"])
                        { Sort = "EventDate DESC" };
                return _intrusionLogView;
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────────
        public CyberarmsSecurityLog() {
            InitializeComponent();
            comboBoxAgentSelection.DisplayMember = "DisplayName";
            comboBoxAgentSelection.ValueMember   = "Id";
            comboBoxAgentSelection.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxAgentSelection.Items.Add(new AgentFilter(new Guid(ALL_AGENTS), "All agents"));
            comboBoxAgentSelection.SelectedIndex = 0;
            comboBoxAgentSelection.SelectionChangeCommitted +=
                new EventHandler(comboBoxAgentSelection_SelectionChangeCommitted);
            dataGridViewIntrusionLog.AutoGenerateColumns = false;
            dataGridViewIntrusionLog.DataSource = IntrusionLogView;
            dataGridViewIntrusionLog.Columns["LogIcon"].DataPropertyName        = "LogIcon";
            dataGridViewIntrusionLog.Columns["LogType"].DataPropertyName        = "LogType";
            dataGridViewIntrusionLog.Columns["LatestEntry"].DataPropertyName    = "EventDate";
            dataGridViewIntrusionLog.Columns["NumberOfEvents"].DataPropertyName = "NumberOfEvents";
            dataGridViewIntrusionLog.Columns["IpAddress"].DataPropertyName      = "IpAddress";
            dataGridViewIntrusionLog.Columns["Agent"].DataPropertyName          = "Message";
            dataGridViewIntrusionLog.Columns["AgentId"].DataPropertyName        = "AgentId";
        }

        // ── Filtro de checkboxes + combo (ahora escribe _baseFilter) ─────────────
        void CyberarmsSecurityLog_FilterSelectionChanged(object sender, EventArgs e) {
            List<string> filter = new List<string>();
            if (!checkBoxFailedLogins.Checked && !checkBoxHardLocks.Checked &&
                !checkBoxSoftLocks.Checked   && !checkBoxSystemMessages.Checked)
                filter.Add("0=1");
            if (checkBoxFailedLogins.Checked)   filter.Add("(Action >99 and Action <200)");
            if (checkBoxSoftLocks.Checked)      filter.Add("(Action >199 and Action <300)");
            if (checkBoxHardLocks.Checked)      filter.Add("(Action >299 and Action <400)");
            if (checkBoxSystemMessages.Checked) filter.Add("(Action >= 500)");

            int i = 0;
            string viewFilter = filter.Count > 0 ? "(" : string.Empty;
            foreach (string f in filter) {
                if (i > 0) viewFilter += " or ";
                viewFilter += f;
                i++;
            }
            if (filter.Count > 0) viewFilter += ")";

            if (comboBoxAgentSelection.Text != null &&
                !((IAgentFilter)comboBoxAgentSelection.SelectedItem).Id.Equals(new Guid(ALL_AGENTS))) {
                viewFilter += (filter.Count > 0 ? " and " : "");
                viewFilter += string.Format("AgentId='{0}'",
                    ((SecurityAgent)comboBoxAgentSelection.SelectedItem).Id);
            }

            _baseFilter = viewFilter;
            ApplyFilter();
        }

        // ── Combina ambos filtros con AND ─────────────────────────────────────────
        private void ApplyFilter() {
            if (string.IsNullOrEmpty(_baseFilter) && string.IsNullOrEmpty(_searchFilter))
                IntrusionLogView.RowFilter = string.Empty;
            else if (string.IsNullOrEmpty(_baseFilter))
                IntrusionLogView.RowFilter = _searchFilter;
            else if (string.IsNullOrEmpty(_searchFilter))
                IntrusionLogView.RowFilter = _baseFilter;
            else
                IntrusionLogView.RowFilter = "(" + _baseFilter + ") AND (" + _searchFilter + ")";
        }

        void comboBoxAgentSelection_SelectionChangeCommitted(object sender, EventArgs e) { }

        // ── Inserción de datos (firma pública idéntica) ───────────────────────────
        public DataRow AddLogEntry(int id, int action, string agentId, Image logIcon,
                                   string logType, DateTime eventDate,
                                   string ipAddress, string message) {
            DataTable t = DataSetIntrusionLog.Tables["IntrusionLog"];
            DataRow row;
            DataRow[] rows = t.Select(string.Format(
                "AgentId='{0}' and IpAddress='{1}' and logType='{2}' and action='{3}'",
                agentId, ipAddress, logType, action));
            if (rows != null && rows.Length > 0) {
                rows[0]["NumberOfEvents"] = int.Parse(rows[0]["NumberOfEvents"].ToString()) + 1;
                rows[0]["EventDate"]      = eventDate;
                rows[0]["EventDateStr"]   = eventDate.ToString("yyyy-MM-dd HH:mm");
                row = rows[0];
            } else {
                row = t.Rows.Add(id, action,
                    SecurityAgents.Instance.GetDisplayName(agentId),
                    logIcon, logType, eventDate, ipAddress, message, agentId, 1,
                    eventDate.ToString("yyyy-MM-dd HH:mm"));
            }
            labelEventsCount.Text = CountEvents().ToString();
            if (MaxLogId < id) MaxLogId = id;
            return row;
        }

        private int CountEvents() {
            int result = 0;
            foreach (DataGridViewRow row in dataGridViewIntrusionLog.Rows) {
                int c;
                if (int.TryParse(row.Cells["NumberOfEvents"].Value.ToString(), out c))
                    result += c;
            }
            return result;
        }

        public DataRow FillLogEntry(int maxId, int action, string agentId, Image logIcon,
                                    string logType, DateTime lastEventDate,
                                    string ipAddress, string message, int numberOfEvents) {
            DataRow row = AddLogEntry(maxId, action, agentId, logIcon, logType,
                                      lastEventDate, ipAddress, message);
            row["NumberOfEvents"] = numberOfEvents;
            labelEventsCount.Text = CountEvents().ToString();
            return row;
        }

        public int MaxLogId { get; set; }

        public void AddAgent(SecurityAgent agent) {
            comboBoxAgentSelection.Items.Add(agent);
        }

        public void RemoveAgent(SecurityAgent agent) {
            try { comboBoxAgentSelection.Items.Remove(agent); } catch { }
        }

        // ── Ordenación por click en cabecera ─────────────────────────────────────
        private void dataGridViewIntrusionLog_ColumnHeaderMouseClick(object sender,
                                                                      DataGridViewCellMouseEventArgs e) {
            DataGridViewColumn col = dataGridViewIntrusionLog.Columns[e.ColumnIndex];
            if (Array.IndexOf(SortableColumns, col.Name) < 0) return;

            string prop = col.DataPropertyName;
            if (_sortColumn == prop)
                _sortAscending = !_sortAscending;
            else {
                _sortColumn    = prop;
                _sortAscending = true;
            }
            IntrusionLogView.Sort = _sortColumn + (_sortAscending ? " ASC" : " DESC");
            UpdateSortIndicators();
        }

        private void UpdateSortIndicators() {
            foreach (DataGridViewColumn col in dataGridViewIntrusionLog.Columns) {
                if (string.IsNullOrEmpty(col.DataPropertyName)) continue;
                string baseText = GetBaseHeaderText(col.Name);
                col.HeaderText = (col.DataPropertyName == _sortColumn)
                    ? baseText + (_sortAscending ? " ▲" : " ▼")
                    : baseText;
            }
        }

        private static string GetBaseHeaderText(string name) {
            switch (name) {
                case "LogType":        return "Type";
                case "LatestEntry":    return "Latest Entry";
                case "NumberOfEvents": return "# of Incidents";
                case "IpAddress":      return "IP-Address";
                case "Agent":          return "Message";
                default:               return name;
            }
        }

        // ── Búsqueda en tiempo real ───────────────────────────────────────────────
        private void textBoxSearch_TextChanged(object sender, EventArgs e) {
            string term = textBoxSearch.Text.Trim();
            if (string.IsNullOrEmpty(term)) {
                _searchFilter = string.Empty;
            } else {
                string safe = term.Replace("'", "''");
                _searchFilter = string.Format(
                    "IpAddress LIKE '%{0}%' OR Agent LIKE '%{0}%' OR " +
                    "Message LIKE '%{0}%' OR EventDateStr LIKE '%{0}%'", safe);
            }
            ApplyFilter();
        }
    }
}
