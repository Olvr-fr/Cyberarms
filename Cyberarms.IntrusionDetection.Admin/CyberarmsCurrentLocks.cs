using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Cyberarms.IntrusionDetection.Shared;

namespace Cyberarms.IntrusionDetection.Admin {
    public partial class CyberarmsCurrentLocks : UserControl {

        // ── DataTable + DataView (mismo patrón que CyberarmsSecurityLog) ─────────
        private DataTable _locksTable;
        private DataView  _locksView;
        private string    _sortColumn    = "LockDate";
        private bool      _sortAscending = false;

        private static readonly string[] SortableColumns = {
            "dataGridViewColumnTypeName",
            "dataGridViewColumnIpAddress",
            "dataGridViewColumnAgent",
            "dataGridViewColumnLockDate",
            "dataGridViewColumnUnlockDate",
        };

        private DataTable LocksTable {
            get {
                if (_locksTable == null) {
                    _locksTable = new DataTable("Locks");
                    _locksTable.Columns.Add("LockId",      typeof(int));
                    _locksTable.Columns.Add("Icon",        typeof(Image));
                    _locksTable.Columns.Add("StatusName",  typeof(string));
                    _locksTable.Columns.Add("ClientIp",    typeof(string));
                    _locksTable.Columns.Add("DisplayName", typeof(string));
                    _locksTable.Columns.Add("LockDate",    typeof(DateTime));
                    _locksTable.Columns.Add("UnlockDate",  typeof(DateTime));
                    _locksTable.Columns.Add("Status",      typeof(int));
                    _locksTable.PrimaryKey = new[] { _locksTable.Columns["LockId"] };
                }
                return _locksTable;
            }
        }

        private DataView LocksView {
            get {
                if (_locksView == null)
                    _locksView = new DataView(LocksTable) { Sort = "LockDate DESC" };
                return _locksView;
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────────
        public CyberarmsCurrentLocks() {
            InitializeComponent();
            dataGridViewLocks.AutoGenerateColumns = false;
            dataGridViewLocks.DataSource = LocksView;
            dataGridViewLocks.Columns["dataGridViewColumnTypeIcon"].DataPropertyName   = "Icon";
            dataGridViewLocks.Columns["dataGridViewColumnTypeName"].DataPropertyName   = "StatusName";
            dataGridViewLocks.Columns["dataGridViewColumnIpAddress"].DataPropertyName  = "ClientIp";
            dataGridViewLocks.Columns["dataGridViewColumnAgent"].DataPropertyName      = "DisplayName";
            dataGridViewLocks.Columns["dataGridViewColumnLockDate"].DataPropertyName   = "LockDate";
            dataGridViewLocks.Columns["dataGridViewColumnUnlockDate"].DataPropertyName = "UnlockDate";
        }

        // ── API pública (firmas idénticas a las originales — IddsAdmin.cs no cambia) ──
        public void Clear() {
            LocksTable.Rows.Clear();
        }

        public void Add(int id, Image icon, string statusName, string clientIp,
                        string displayName, DateTime lockDate, DateTime unlockDate, int status) {
            DataRow[] found = LocksTable.Select("LockId = " + id);
            if (found.Length > 0) {
                DataRow r = found[0];
                if ((int)r["Status"] == status) return;   // sin cambio real: evitar repintado
                r.BeginEdit();
                r["StatusName"] = statusName;
                r["Status"]     = status;
                r["UnlockDate"] = unlockDate;
                r.EndEdit();
            } else {
                LocksTable.Rows.Add(id, icon, statusName, clientIp, displayName, lockDate, unlockDate, status);
            }
        }

        public void SetHardLocks(int number) {
            labelCurrentLocksHardLocks.Text = string.Format("{0} hard locks", number);
        }

        public void SetSoftLocks(int number) {
            labelCurrentLocksSoftLocks.Text = string.Format("{0} soft locks", number);
        }

        // ── Select-all checkbox ──────────────────────────────────────────────────
        private void checkBoxSelectAllLocks_CheckedChanged(object sender, EventArgs e) {
            foreach (DataGridViewRow r in dataGridViewLocks.Rows)
                ((DataGridViewCheckBoxCell)r.Cells["dataGridViewSelectItem"]).Value =
                    ((CheckBox)sender).Checked;
        }

        // ── Acción de desbloqueo ─────────────────────────────────────────────────
        private void actionMenu_MouseDown(object sender, MouseEventArgs e) {
            Control c = (Control)sender;
            c.Location = new Point(c.Location.X + 1, c.Location.Y + 1);
        }

        private void actionMenu_MouseUp(object sender, MouseEventArgs e) {
            Control c = (Control)sender;
            c.Location = new Point(c.Location.X - 1, c.Location.Y - 1);
        }

        private void actionMenuUnlock_Click(object sender, EventArgs e) {
            foreach (DataGridViewRow row in dataGridViewLocks.Rows) {
                var cb = (DataGridViewCheckBoxCell)row.Cells["dataGridViewSelectItem"];
                if ((bool)cb.EditedFormattedValue != true) continue;

                var drv = row.DataBoundItem as DataRowView;
                if (drv == null) continue;

                int status = (int)drv["Status"];
                if (status != (int)Lock.LOCK_STATUS_SOFTLOCK &&
                    status != (int)Lock.LOCK_STATUS_HARDLOCK) continue;

                Lock l = Locks.GetLockById((int)drv["LockId"]);
                if (l != null) {
                    l.Status = Lock.LOCK_STATUS_UNLOCK_REQUESTED;
                    l.Save();
                }
                drv.BeginEdit();
                drv["StatusName"] = LockStatusAdapter.GetLockStatusName((int)Lock.LOCK_STATUS_MANUAL);
                drv["Status"]     = (int)Lock.LOCK_STATUS_MANUAL;
                drv.EndEdit();
            }
        }

        // ── Ordenación por click en cabecera ─────────────────────────────────────
        private void dataGridViewLocks_ColumnHeaderMouseClick(object sender,
                                                               DataGridViewCellMouseEventArgs e) {
            DataGridViewColumn col = dataGridViewLocks.Columns[e.ColumnIndex];
            if (Array.IndexOf(SortableColumns, col.Name) < 0) return;

            if (_sortColumn == col.DataPropertyName)
                _sortAscending = !_sortAscending;
            else {
                _sortColumn    = col.DataPropertyName;
                _sortAscending = true;
            }

            LocksView.Sort = _sortColumn + (_sortAscending ? " ASC" : " DESC");
            UpdateSortIndicators();
        }

        private void UpdateSortIndicators() {
            foreach (DataGridViewColumn col in dataGridViewLocks.Columns) {
                if (string.IsNullOrEmpty(col.DataPropertyName)) continue;
                string baseText = GetBaseHeaderText(col.Name);
                col.HeaderText = (col.DataPropertyName == _sortColumn)
                    ? baseText + (_sortAscending ? " ▲" : " ▼")
                    : baseText;
            }
        }

        private static string GetBaseHeaderText(string name) {
            switch (name) {
                case "dataGridViewColumnTypeName":   return "Type";
                case "dataGridViewColumnIpAddress":  return "IP-Address";
                case "dataGridViewColumnAgent":      return "Agent / Attacked System";
                case "dataGridViewColumnLockDate":   return "Date of Lock";
                case "dataGridViewColumnUnlockDate": return "Automatic Unlock";
                default: return name;
            }
        }

        // ── Búsqueda en tiempo real ──────────────────────────────────────────────
        private void textBoxSearch_TextChanged(object sender, EventArgs e) {
            string term = textBoxSearch.Text.Trim();
            if (string.IsNullOrEmpty(term)) {
                LocksView.RowFilter = string.Empty;
                return;
            }
            string safe = term.Replace("'", "''");
            LocksView.RowFilter = string.Format(
                "ClientIp LIKE '%{0}%' OR DisplayName LIKE '%{0}%' OR StatusName LIKE '%{0}%'", safe);
        }
    }
}
