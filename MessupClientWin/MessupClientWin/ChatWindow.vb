Public Class ChatWindow
    Public chatInfo As Chat
    Public lastMessageNo As Integer = 0
    Public accountId As Integer = Nothing

    Public Sub New(chatInfo As Chat, accountId As Integer)
        Me.chatInfo = chatInfo
        Me.accountId = accountId
        InitializeComponent()
    End Sub

    Private Sub ChatWindow_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        WebBrowser1.Navigate(String.Empty)
        WebBrowser1.Document.Write("<head><style>* {font-family: 'Arial', sans-serif; } body { font-size: 11px; padding: 0px; margin: 4px;} .pc { color: #c00; } .pa { color: #0c0; } .hd { font-weight: bold; }</style></head><body>")
        checkForNewMessages()
        refreshInformation()
    End Sub

    Public Sub refreshInformation()
        LabelName.Text = chatInfo.name
        LabelEmail.Text = chatInfo.email
        LabelStarted.Text = chatInfo.starttime
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        checkForNewMessages()
    End Sub

    Private Sub checkForNewMessages()
        ' check for new messages
        Dim postData As String = "form=checkmsg&convid=" + chatInfo.id + "&latestMessage=" + lastMessageNo.ToString
        Program.MainForm.SendAsynchRequest(Program.MainForm.chataccounts2(accountId).url, postData, accountId)
    End Sub

    Public Sub addMessages(msgs As List(Of chatMessage))
        Dim newIncomingMessage As Boolean = False
        If Not msgs Is Nothing Then
            Dim msg As String = Nothing
            For i As Integer = 0 To msgs.Count - 1
                msg = "<span class='hd'>" + msgs(i).time + " - <span class='p" + msgs(i).who + "'>" + msgs(i).name + "</span>:</span> " + msgs(i).message + "<br />"
                WebBrowser1.Document.Write(msg)
                WebBrowser1.Document.Body.All(WebBrowser1.Document.Body.All.Count - 1).ScrollIntoView(False)
                lastMessageNo = msgs(i).id
                If msgs(i).who = "c" Then
                    newIncomingMessage = True
                End If
            Next
            If newIncomingMessage = True Then
                Program.MainForm.announceMsg()
            End If
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        sendmsg()
    End Sub

    Private Sub sendmsg()
        Dim postData As String = "form=sendmsg&convid=" + chatInfo.id + "&latestMessage=" + lastMessageNo.ToString + "&message=" + RichTextBox2.Text
        Program.MainForm.SendAsynchRequest(Program.MainForm.chataccounts2(accountId).url, postData, accountId)
        RichTextBox2.Text = ""
    End Sub

    Private Sub RichTextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles RichTextBox2.KeyDown
        If e.KeyCode = Keys.Enter Then
            sendmsg()
            e.Handled = True
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        endchat()
    End Sub

    Private Sub endchat()
        Dim msgbxr As MsgBoxResult = MsgBox("Really end this chat?", MsgBoxStyle.YesNo, "End chat")
        If msgbxr = MsgBoxResult.Yes Then
            Dim postData As String = "form=endchat&convid=" + chatInfo.id
            Program.MainForm.SendAsynchRequest(Program.MainForm.chataccounts2(accountId).url, postData, accountId)
        End If
        Me.Dispose()
    End Sub
End Class