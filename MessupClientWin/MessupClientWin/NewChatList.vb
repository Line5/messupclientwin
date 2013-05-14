Public Class NewChatList

    Private Sub NewChatList_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        MdiParent = Program.MainForm
        DataGridViewNewChats.AllowUserToAddRows = False
        DataGridViewNewChats.AllowUserToDeleteRows = False
        DataGridViewNewChats.Columns.Add("colName", "Name")
        DataGridViewNewChats.Columns.Add("colEmail", "Email")
        DataGridViewNewChats.Columns.Add("colTime", "Time")
        DataGridViewNewChats.Columns.Add("colMsg", "Message")
        DataGridViewNewChats.Columns.Add("colId", "Id")
        DataGridViewNewChats.Columns.Add("colAccountId", "AccountId")

    End Sub

    Private Sub DataGridViewNewChats_CellMouseDoubleClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DataGridViewNewChats.CellMouseDoubleClick
        Dim chatid As String = DataGridViewNewChats.Rows(e.RowIndex).Cells("colId").Value

        Dim accountid As String = DataGridViewNewChats.Rows(e.RowIndex).Cells("colAccountId").Value
        If Not Program.MainForm.chatWindows.ContainsKey(chatid) Then
            Program.MainForm.chatWindows(chatid) = New ChatWindow(Program.MainForm.chatInformation(accountid + "-" + chatid), accountid)
            Program.MainForm.chatWindows(chatid).MdiParent = Program.MainForm
        ElseIf Program.MainForm.chatWindows(chatid).IsDisposed Then
            Program.MainForm.chatWindows(chatid) = New ChatWindow(Program.MainForm.chatInformation(accountid + "-" + chatid), accountid)
            Program.MainForm.chatWindows(chatid).MdiParent = Program.MainForm
        Else
            Program.MainForm.chatWindows(chatid).Focus()
        End If
        'Display the new form.
        Program.MainForm.chatWindows(chatid).Show()
    End Sub

End Class