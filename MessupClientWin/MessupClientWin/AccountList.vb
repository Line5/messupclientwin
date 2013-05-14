Imports System.Net
Imports System.Threading

Public Class AccountList

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim dt As DataTable = CType(DataGridView1.DataSource, DataTable)
        dt.AcceptChanges()
        dt.WriteXml(Program.MainForm.settingspath + "messupclientconfig.xml", System.Data.XmlWriteMode.WriteSchema, False)
        Close()
        Me.Dispose()
    End Sub

    Private Sub AccountList_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim dt As New DataTable("account")
        dt.Columns.Add("ScriptPath", GetType(String))
        dt.Columns.Add("Username", GetType(String))
        dt.Columns.Add("Password", GetType(String))

        With DataGridView1
            .DataSource = dt
            .AllowUserToAddRows = True : .AllowUserToDeleteRows = True
            .AllowUserToOrderColumns = False : .AllowUserToResizeRows = True
        End With

        Try
            ' Dim dt As New DataTable
            dt.ReadXml(Program.MainForm.settingsPath + "messupclientconfig.xml")
            DataGridView1.DataSource = dt
        Catch ex As Exception

        End Try


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Close()
    End Sub


   
End Class

