Imports System.Net
Imports System.Threading
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports System.Collections.Hashtable
Imports System.Xml

' known issues:
' - config is only read after restart of program

Public Class Form1
    ''' <summary>
    ''' contains the cookies for all accounts
    ''' </summary>
    ''' <remarks></remarks>
    Dim cookies As New Dictionary(Of Integer, CookieContainer)
    Dim knownChats As New Hashtable
    Public chatInformation As New Dictionary(Of String, Chat)
    Public chatWindows As New Dictionary(Of String, ChatWindow)
    Dim chataccounts As New List(Of ChatAccount)
    Public chataccounts2 As New Dictionary(Of Integer, ChatAccount)
    Public AccountStatusWindow As New AccountStatus
    Public AccountListWindow As New AccountList
    Public NewChatListWindow As New NewChatList

    ''' <summary>
    ''' determines if sound notifications are to be used.
    ''' </summary>
    ''' <remarks>The value of this variable is stored permanently on a per-user base
    ''' within the my.settings. It can be changed via the "Options" menu.</remarks>
    Public soundOn As Boolean = True

    Public settingsPath As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\MessupClientWin\"

    Private Sub AccountsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AccountsToolStripMenuItem.Click
        AccountListWindow.Show()
    End Sub

    Private Sub Form1_Click(sender As Object, e As EventArgs) Handles Me.Click
        stopBlinking()
    End Sub

    Private Sub stopBlinking()
        FlashWindow(Me.Handle, FLASHW_STOP, 5, 1000)
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1 - load config
        AccountStatusWindow.MdiParent = Me
        AccountStatusWindow.Show()
        readConfig()
        If My.Settings.soundOn = True Then
            soundOn = True
            Me.SoundNotificationToolStripMenuItem.Checked = True
        Else
            soundOn = False
            Me.SoundNotificationToolStripMenuItem.Checked = False
        End If
        ' 2 - start login process to all chats
        logAllIn()
        ' 3 - load open, assigned chats 
        ' 4 - load new, unassigned chats + auto refresh
        NewChatListWindow.Show()
    End Sub

    Private Sub checkForOpenChats()

    End Sub

    ' Runs within the main thread.
    ' Just calls the "poolRequest" function.
    Public Sub SendAsynchRequest(ByVal url As String, ByVal postData As String, accountid As Integer)
        Dim myRequest As New RequestSendParams
        myRequest.url = url
        myRequest.postData = postData
        poolRequest(myRequest, accountid)
    End Sub

    ' Runs within the main thread.
    ' pools a request.
    Public Sub poolRequest(myReq As RequestSendParams, accountid As Integer)
        ThreadPool.QueueUserWorkItem(Sub(state)
                                         SendThreadedRequest(myReq, accountid)
                                     End Sub, myReq)
        'ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf SendThreadedRequest), myReq)
    End Sub

    ''' <summary>
    ''' Starts the http request. This function is to be called within a background thread.
    ''' </summary>
    ''' <param name="myObj"></param>
    ''' <param name="accountid"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function SendThreadedRequest(ByVal myObj As Object, accountid As Integer)
        Dim myText As String = ""
        Try

            Dim params As RequestSendParams = myObj

            Dim request As HttpWebRequest

            ' Create the request

            request = CType(WebRequest.Create(params.url + "?type=ajax"), HttpWebRequest)
            request.ContentType = "application/x-www-form-urlencoded"
            CType(request, HttpWebRequest).UserAgent = "MESSUP"
            request.Method = "POST"

            request.CookieContainer = cookies(accountid)

            ' Convert the xml doc string into a byte array
            Dim bytes As Byte()
            bytes = Encoding.UTF8.GetBytes(params.postData)

            ' Assign the content length
            request.ContentLength = bytes.Length

            ' Write the xml doc bytes to request stream
            request.GetRequestStream.Write(bytes, 0, bytes.Length)

            Dim state As WebRequestState

            ' Create the state object used to access the web request
            state = New WebRequestState(request)

            Try
                Dim response As WebResponse = request.GetResponse()
                Dim dataStream As Stream = response.GetResponseStream()

                Dim encode As Encoding = System.Text.Encoding.GetEncoding("utf-8")
                Dim readStream As New StreamReader(dataStream, encode)
                Dim read(256) As [Char]
                Dim count As Integer = readStream.Read(read, 0, 256)
                While count > 0

                    ' Dump the 256 characters on a string and display the string onto the console. 
                    Dim str As New [String](read, 0, count)
                    myText += str
                    count = readStream.Read(read, 0, 256)

                End While
                dataStream.Close()

                response.Close()
                Me.Invoke(New handleResponseInvoker(AddressOf handleResponse), myText)
            Catch ex As WebException

            Catch ex As Exception

            End Try

        Catch ex As Exception
            '
        End Try
        Return myText
    End Function


    ' Method called when a request times out
    Private Sub TimeoutCallback(ByVal state As Object, ByVal timeOut As Boolean)
        If (timeOut) Then
            ' Abort the request
            CType(state, WebRequestState).Request.Abort()
        End If
    End Sub

    ' Stores web request for access during async processing
    Private Class WebRequestState
        ' Holds the request object
        Public Request As WebRequest

        Public Sub New(ByVal newRequest As WebRequest)
            Request = newRequest
        End Sub
    End Class

    Private Sub fillNewChatsBox(chats As List(Of Chat), accountid As Integer)

        Dim myOnlineChats As New Dictionary(Of String, Integer)
        ' first, write all chats of this account to the variable
        Dim splitstrings() As String
        For Each element As DictionaryEntry In knownChats
            splitstrings = element.Key.ToString.Split("-")
            If splitstrings(0) = accountid Then
                myOnlineChats.Add(element.Key.ToString, 1)
            End If
        Next

        If Not chats Is Nothing Then
            For i As Integer = 0 To chats.Count - 1
                If knownChats.Item(accountid.ToString + "-" + chats(i).id) Is Nothing Then
                    NewChatListWindow.DataGridViewNewChats.Rows.Add(New String() {chats(i).name, chats(i).email, chats(i).starttime, chats(i).msg, chats(i).id, accountid})
                    knownChats.Add(accountid.ToString + "-" + chats(i).id, 1)
                    chatInformation.Add(accountid.ToString + "-" + chats(i).id, chats(i))
                    NewChatListWindow.DataGridViewNewChats.Refresh()
                    FlashWindow(Me.Handle, FLASHW_ALL, 5, 1000)
                    announceMsg()
                End If
                myOnlineChats.Remove(accountid.ToString + "-" + chats(i).id)
            Next i
        End If
        If myOnlineChats.Count > 0 Then
            Dim pair As KeyValuePair(Of String, Integer)

            Dim splitpos As Integer
            Dim xAccountId As Integer
            Dim xChatId As String
            For Each pair In myOnlineChats
                knownChats.Remove(pair.Key)
                splitpos = Strings.InStr(pair.Key, "-")
                xChatId = pair.Key.Substring(splitpos)
                xAccountId = Int(pair.Key.Substring(0, splitpos - 1))
                chatInformation.Remove(pair.Key)
                For Each row As DataGridViewRow In NewChatListWindow.DataGridViewNewChats.Rows
                    If row.Cells("colAccountId").Value.ToString = xAccountId.ToString And row.Cells("colId").Value = xChatId Then
                        NewChatListWindow.DataGridViewNewChats.Rows.RemoveAt(row.Index)
                    End If
                Next
            Next
        End If
    End Sub

    ' reads accounts from an xml file and stores them in some vars
    Private Sub readConfig()
        Dim m_xmld As XmlDocument
        Dim m_nodelist As XmlNodeList
        Dim m_node As XmlNode
        Try
            If Not Directory.Exists(Me.settingsPath) Then
                Directory.CreateDirectory(Me.settingsPath)
            End If
            m_xmld = New XmlDocument()
            m_xmld.Load(Me.settingsPath + "messupclientconfig.xml")
            m_nodelist = m_xmld.SelectNodes("/NewDataSet/account")
            Dim i As Integer = 1
            For Each m_node In m_nodelist
                Dim acc As New ChatAccount
                acc.username = m_node.SelectNodes("Username").Item(0).InnerText
                acc.password = m_node.SelectNodes("Password").Item(0).InnerText
                acc.url = m_node.SelectNodes("ScriptPath").Item(0).InnerText
                chataccounts.Add(acc)
                chataccounts2.Add(i, acc)
                AccountStatusWindow.DataGridView1.Rows.Add({i, acc.url, acc.username})
                i += 1
            Next

        Catch ex As Exception

        End Try
    End Sub

    ' starts login functionality for all known accounts.
    Private Sub logAllIn()
        For Each kvp As KeyValuePair(Of Integer, ChatAccount) In chataccounts2
            If Not cookies.ContainsKey(kvp.Key) Then
                Dim mcc As CookieContainer = New CookieContainer()
                'If accountid < 2 Then
                cookies.Add(kvp.Key, mcc)
                'End If
            End If
            Dim postData As String = "form=agentlogin&account=" + kvp.Key.ToString + "&email=" + kvp.Value.username + "&pass=" + kvp.Value.password
            AccountStatusWindow.DataGridView1.Rows(kvp.Key - 1).Cells("status").Value = "Loading..."
            SendAsynchRequest(kvp.Value.url, postData, kvp.Key)

        Next
    End Sub

    ' updates the login status within the account status list.
    Private Sub updateAgentLoginStatus(res As String, account As Integer)
        Dim val As String
        Dim col As Color
        If res = "OK" Then
            val = "OK"
            col = Color.LightGreen
        Else
            val = "Login failed"
            col = Color.Red
        End If
        chataccounts2(account).status = val
        AccountStatusWindow.DataGridView1.Rows(account - 1).Cells("status").Value = val
        AccountStatusWindow.DataGridView1.Rows(account - 1).Cells("status").Style.BackColor = col
        Me.Refresh()
        If val = "OK" Then
            checkAccountForNewChats(account)
            If TimerCheckNewChats.Enabled = False Then
                TimerCheckNewChats.Enabled = True
            End If
        End If
    End Sub

    ' This is not called within the main thread!
    Public Event ReceiveResponse(ByVal response As String)

    ' This is not called within the main thread!
    Protected Sub Form1_ReceiveResponse(ByVal response As String) Handles Me.ReceiveResponse
        handleResponse(response)
    End Sub

    ' The special delegate is needed to call the function in the main thread from a background thread
    Public Delegate Sub handleResponseInvoker(ByVal response As String)

    ' This runs within the main thread, but is called from a background thread, using the delegate.
    Protected Sub handleResponse(response As String)
        Dim jss As New JavaScriptSerializer
        'Dim dict = jss.Deserialize(Of List(Of jsonAnswer))(myText)
        Dim myAnswer As jsonAnswer = jss.Deserialize(Of jsonAnswer)(response)
        If response.Length > 3 Then
            Dim answerForm As String = Nothing
            If Not myAnswer.form Is Nothing Then
                answerForm = myAnswer.form
            End If
            Select Case answerForm
                Case "agentlogin"
                    updateAgentLoginStatus(myAnswer.res, myAnswer.account)
                Case "checkmsg"
                    updateChatWindow(myAnswer.messages, myAnswer.account, myAnswer.convid)
                Case "newchats"
                    fillNewChatsBox(myAnswer.chats, myAnswer.account)
                    AccountStatusWindow.DataGridView1.Rows(myAnswer.account - 1).Cells("status").Value = "OK"
            End Select
        End If
    End Sub

    Private Sub TimerCheckNewChats_Tick(sender As Object, e As EventArgs) Handles TimerCheckNewChats.Tick
        checkAllAccountsForNewChats()
    End Sub

    Private Sub checkAllAccountsForNewChats()
        For Each kvp As KeyValuePair(Of Integer, ChatAccount) In chataccounts2
            checkAccountForNewChats(kvp.Key)
        Next
    End Sub

    Private Sub checkAccountForNewChats(accountid As Integer)
        ' check if logged in ...
        Dim kvp As ChatAccount = chataccounts2(accountid)
        If chataccounts2(accountid).status = "OK" Then
            Dim postData As String = "form=newchats&account=" + accountid.ToString ' add account id
            AccountStatusWindow.DataGridView1.Rows(accountid - 1).Cells("status").Value = "reading..."
            AccountStatusWindow.DataGridView1.Refresh()
            SendAsynchRequest(kvp.url, postData, accountid)
        Else
            'Console.WriteLine("Account " + accountid.ToString + " is not logged in.")
        End If
    End Sub

    Private Sub checkAllAccountsForAllChats()
        For Each kvp As KeyValuePair(Of Integer, ChatAccount) In chataccounts2
            ' check if logged in ...
            If chataccounts2(kvp.Key).status = "OK" Then
                Dim postData As String = "form=chats&account=" + kvp.Key.ToString ' add account id
                AccountStatusWindow.DataGridView1.Rows(kvp.Key - 1).Cells("status").Value = "reading..."
                AccountStatusWindow.DataGridView1.Refresh()
                SendAsynchRequest(kvp.Value.url, postData, kvp.Key)
            Else
                'Console.WriteLine("Account " + kvp.Key.ToString + " is not logged in.")
            End If
        Next
    End Sub

    Private Sub updateChatWindow(msgs As List(Of chatMessage), accountid As Integer, chatid As String)
        chatWindows(chatid).addMessages(msgs)
    End Sub

    Private Declare Auto Function FlashWindowEx Lib "user32" _
    (ByRef FWI As FLASHWINFO) As Boolean

    ''' <summary>end blinking</summary>
    Public Const FLASHW_STOP = 0
    ''' <summary>title bar flahes</summary>
    Public Const FLASHW_CAPTION = &H1
    ''' <summary>taskbar flashes</summary>
    Public Const FLASHW_TRAY = &H2
    ''' <summary>combine title bar + task bar</summary>
    Public Const FLASHW_ALL = (FLASHW_CAPTION Or FLASHW_TRAY)

    Private Structure FLASHWINFO
        Dim cbSize As UInt16
        Dim hwnd As IntPtr
        Dim dwFlags As UInt32
        Dim uCount As UInt16
        Dim dwTimeout As UInt32
    End Structure

    ''' <summary>
    ''' window blinking
    ''' </summary>
    ''' <param name="Handle">window handle</param>
    ''' <param name="FlashMode">blink mode</param>
    ''' <param name="FlashCount">determines how often to repeat</param>
    ''' <param name="Speed">blinking interval in ms</param>
    Public Sub FlashWindow(ByVal Handle As Integer, ByVal FlashMode As Integer, ByVal FlashCount As Integer, Optional ByVal Speed As Integer = 0)
        Dim FlashInfo As New FLASHWINFO
        FlashInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(FlashInfo)
        FlashInfo.dwFlags = FlashMode
        FlashInfo.dwTimeout = Speed
        FlashInfo.hwnd = Handle
        FlashInfo.uCount = FlashCount
        FlashWindowEx(FlashInfo)
    End Sub

    Public Sub announceMsg()
        If soundOn = True Then
            My.Computer.Audio.Play("dingding.wav")
        End If
    End Sub

    Private Sub SoundNotificationToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SoundNotificationToolStripMenuItem.Click
        If soundOn = True Then
            soundOn = False
            My.Settings.soundOn = False
            SoundNotificationToolStripMenuItem.Checked = False
        Else
            soundOn = True
            My.Settings.soundOn = True
            SoundNotificationToolStripMenuItem.Checked = True
        End If
        My.Settings.Save()
    End Sub
End Class

Public Class jsonAnswer
    ' error message
    Public err As String = Nothing
    ' form
    Public form As String = Nothing
    Public chats As List(Of Chat)
    ' id of the account (internal, client-related id), equals dictionary key
    Public account As Integer
    Public convid As String
    Public res As String = Nothing
    Public messages As List(Of chatMessage)
End Class

Public Class chatMessage
    Public id As Integer
    Public time As String
    Public name As String
    Public message As String
    Public who As String
End Class

Public Class Chat
    Public id As String = Nothing
    Public name As String = Nothing
    Public email As String = Nothing
    Public msg As String = Nothing
    Public starttime As String = Nothing
    Public account As Integer = Nothing
End Class

Public Class ChatAccount
    Public username As String = Nothing
    Public password As String = Nothing
    Public url As String = Nothing
    Public status As String = "-"
End Class

Public Class RequestSendParams
    Public url As String = Nothing
    Public postData As String = Nothing
End Class

Public Module Program
    Private m_MainForm As Form1

    Public ReadOnly Property MainForm() As Form1
        Get
            Return m_MainForm
        End Get
    End Property

    Public Sub Main()
        m_MainForm = New Form1()
        Application.Run(m_MainForm)
    End Sub
End Module