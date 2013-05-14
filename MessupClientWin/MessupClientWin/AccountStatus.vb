Public Class AccountStatus

    Private Sub AccountStatus_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        MdiParent = Program.MainForm
        DataGridView1.Columns.Add("id", "Id")
        DataGridView1.Columns.Add("site", "Site")
        DataGridView1.Columns.Add("account", "Account")
        DataGridView1.Columns.Add("status", "Status")
    End Sub
End Class