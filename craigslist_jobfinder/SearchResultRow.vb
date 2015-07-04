Imports Microsoft.VisualBasic

<Serializable()>
Public Class SearchResultRow
    Dim ahref As String
    Dim postingDate As String
    Dim linkText As String

    Public Sub New(iAhref As String, iPostingDate As String, iLinkText As String)
        ahref = iAhref
        postingDate = iPostingDate
        linkText = iLinkText
    End Sub

    Public Function getData() As String
        Return ahref + " " + postingDate + " " + linkText
    End Function

    Public Function getURI() As String
        Return ahref
    End Function

    Public Function getPostingDate() As String
        Return postingDate
    End Function

    Public Function getText() As String
        Return linkText
    End Function
End Class