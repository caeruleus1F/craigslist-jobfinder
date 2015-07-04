Imports System.Net
Imports System
Imports System.Net.Mail
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.ComponentModel

Public Class Main
    Dim listSearchResults As List(Of SearchResultRow)
    Dim listExistingResults As List(Of SearchResultRow)
    Dim baseUri As String = "http://klamath.craigslist.org"
    Dim client As New WebClient()

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        queryServer()
        SearchTimer.Enabled = True
    End Sub

    Public Sub queryServer()
        Try
            client.DownloadStringAsync(New Uri(baseUri + "/search/ggg?sort=date"))
            rtbDisplay.Text = "Last attempt: " & Date.Now & vbNewLine &
                "Next attempt: " & Date.Now.AddMinutes(5) & vbNewLine & vbNewLine
        Catch ex As Exception
        End Try
    End Sub

    Public Sub processResponse(sender As Object, e As DownloadStringCompletedEventArgs)
        If e.Result.Contains("<div class=""content"">") Then
            Try
                Dim strTemp As String = e.Result
                Dim searchResults As String = Nothing
                Dim indexOfFirstDiv As Integer = strTemp.IndexOf("<div class=""content"">")
                Dim indexOfSecondDiv As Integer = strTemp.IndexOf("<div", indexOfFirstDiv + 1)
                Dim length As Integer = indexOfSecondDiv - indexOfFirstDiv
                Dim keepLooping As Boolean = True

                searchResults = strTemp.Substring(indexOfFirstDiv, length)

                While keepLooping
                    If searchResults.IndexOf("<p class=""row""") >= 0 Then
                        Dim firstIndex As Integer = searchResults.IndexOf("<p class=""row""")
                        Dim endIndex As Integer = searchResults.IndexOf("</p>")
                        Dim row As String = searchResults.Substring(firstIndex, endIndex - firstIndex + 4)

                        Dim linkFirstIndex As Integer = row.LastIndexOf("a href=""") + 8
                        Dim linkEndIndex As Integer = row.LastIndexOf(".html")
                        Dim link As String = row.Substring(linkFirstIndex, linkEndIndex - linkFirstIndex + 5)

                        Dim dateFirstIndex As Integer = row.LastIndexOf("class=""date"">") + 13
                        Dim dateEndIndex As Integer = row.IndexOf("</span>", dateFirstIndex)
                        Dim postDate As String = row.Substring(dateFirstIndex, dateEndIndex - dateFirstIndex)

                        Dim textFirstIndex As Integer = row.LastIndexOf("class=""hdrlnk"">") + 15
                        Dim textEndIndex As Integer = row.IndexOf("</a>", textFirstIndex)
                        Dim text As String = row.Substring(textFirstIndex, textEndIndex - textFirstIndex)

                        If link.Contains("http://") Then
                            Dim tempSRR As SearchResultRow = New SearchResultRow(link, postDate, text)
                            listSearchResults.Add(tempSRR)
                        Else
                            Dim tempSRR As SearchResultRow = New SearchResultRow(baseUri & link, postDate, text)
                            listSearchResults.Add(tempSRR)
                        End If

                        searchResults = searchResults.Remove(0, endIndex + 5)
                    Else
                        keepLooping = False
                    End If
                End While
            Catch ex As Exception
            End Try

            'updateListView()
            displayResults()
            checkForNewResults()
        End If
    End Sub

    Public Sub displayResults()
        Try
            For i = 0 To listSearchResults.Count - 1
                rtbDisplay.Text += listSearchResults.Item(i).getPostingDate() & " " & listSearchResults.Item(i).getURI & " " & listSearchResults.Item(i).getText() & vbNewLine
            Next
        Catch ex As Exception
        End Try
    End Sub

    'Public Sub updateListView()
    '    Dim lvi As ListViewItem
    '    lsvDisplay.BeginUpdate()
    '    For i = 0 To listSearchResults.Count - 1
    '        lvi = lsvDisplay.Items.Add(listSearchResults.Item(i).getPostingDate())
    '        lvi.SubItems.Add(listSearchResults.Item(i).getText())
    '        'Dim tempLinkLabel As LinkLabel
    '        'With tempLinkLabel
    '        '    .BackColor = Color.White
    '        '    .Text = listSearchResults.Item(i).getURI()
    '        '    .Visible = True
    '        'End With
    '        lvi.SubItems.Add(listSearchResults.Item(i).getURI())
    '        'lvi.SubItems.Add(listSearchResults.Item(i).getText())
    '    Next
    '    lsvDisplay.Update()
    '    lsvDisplay.EndUpdate()
    '
    'End Sub

    Public Sub checkForNewResults()
        Dim match As Boolean
        Dim formatter As New BinaryFormatter()

        Try
            Dim readStream As New FileStream(My.Resources.InternalStorage.results, FileMode.Open, FileAccess.Read, FileShare.Read)
            listExistingResults.Clear()
            listExistingResults = CType(formatter.Deserialize(readStream), List(Of SearchResultRow))
            readStream.Close()
        Catch ex As Exception
        End Try

        Try
            If Not listExistingResults.Count = 0 Then
                For i = 0 To listSearchResults.Count - 1
                    match = False
                    For j = 0 To listExistingResults.Count - 1
                        If listSearchResults.Item(i).getData = listExistingResults.Item(j).getData Then
                            match = True
                        End If
                    Next

                    If match = False And chbText.Checked = True Then
                        sendText(i)
                    End If
                Next
            End If
        Catch ex As Exception
        End Try

        Try
            Dim writeStream As New FileStream(My.Resources.InternalStorage.results, FileMode.Create, FileAccess.ReadWrite, FileShare.None)
            formatter.Serialize(writeStream, listSearchResults)
            writeStream.Close()
        Catch ex As Exception
        End Try

        listSearchResults.Clear()
    End Sub

    Public Sub sendText(index As Integer)
        Try
            Dim fromAddress As String = "txtmsg0001@gmail.com"
            Dim password As String = "textmessage"
            Dim subject As String = listSearchResults.Item(index).getText
            Dim message As String = listSearchResults.Item(index).getURI
            Dim phoneAddress As String = "7022905432@txt.att.net"
            Dim mail As New MailMessage(fromAddress, phoneAddress, subject, message)
            Dim smtpClient As New SmtpClient("smtp.gmail.com", 587)

            'AddHandler smtpClient.SendCompleted, AddressOf somesub
            smtpClient.UseDefaultCredentials = False
            smtpClient.Credentials = New Net.NetworkCredential(fromAddress, password)
            smtpClient.EnableSsl = True
            smtpClient.SendMailAsync(mail)
        Catch ex As Exception
            rtbDisplay.Text = ex.Message
        End Try
    End Sub

    'Public Sub somesub(sender As Object, e As AsyncCompletedEventArgs)
    '
    'End Sub

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        client.Proxy() = Nothing
        AddHandler client.DownloadStringCompleted, AddressOf processResponse
        listSearchResults = New List(Of SearchResultRow)
        listExistingResults = New List(Of SearchResultRow)
    End Sub

    Private Sub SearchTimer_Tick(sender As Object, e As EventArgs) Handles SearchTimer.Tick
        queryServer()
    End Sub

    Private Sub rtbDisplay_LinkClicked(sender As Object, e As LinkClickedEventArgs) Handles rtbDisplay.LinkClicked
        System.Diagnostics.Process.Start(e.LinkText)
    End Sub
End Class
