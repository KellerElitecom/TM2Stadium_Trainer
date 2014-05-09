Public Class Form1
    Dim maniaplanetbase As Integer
    Dim maniaplanetsize As Integer
    Dim injectionaddress As Integer
    Dim dirtoriginaddress As Integer

    Private Sub time_check_Tick(sender As Object, e As EventArgs) Handles time_check.Tick
        If UpdateProcessHandle() Then
            lbl_status.Text = "TM2 found!"
            lbl_status.ForeColor = Color.LimeGreen
        Else
            lbl_status.Text = "Waiting for TM2..."
            lbl_status.ForeColor = Color.Red
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        RemoveProtection(&H400310, 100)

    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        maniaplanetbase = GetModuleInfo("maniaplanet.exe", False)
        maniaplanetsize = GetModuleInfo("maniaplanet.exe", True)
        ' \x66\x8B\x47\x0C\x66\x89\x46\x78\x8B\x47", "xxx?xxx?xx");
        Dim dirtpattern As Byte() = {&H66, &H8B, &H47, &HC, &H66, &H89, &H46, &H78, &H8B, &H47}
        Dim dirtmask As String = "xxx?xxx?xx"
        Dim granddump As Byte() = ReadMemory(Of Byte())(maniaplanetbase, maniaplanetsize, False)


        Dim dirtasm As Integer = patternscan(0, dirtpattern, dirtmask, granddump)
        dirtoriginaddress = dirtasm + maniaplanetbase


        
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        injectionaddress = maniaplanetbase + &H310
        Dim injection As Byte() = {&H66, &H8B, &H47, &HC, &H66, &H3D, &H2, &H0, &HF, &H84, &HA, &H0, &H0, &H0, &H66, &H3D, &H6, &H0, &HF, &H85, &H4, &H0, &H0, &H0, &H66, &HB8, &HA, &H0, &H66, &H89, &H46, &H78}
        Dim detouraddy As Byte() = BitConverter.GetBytes(injectionaddress - 5 - dirtoriginaddress)
        Dim E9 As Byte() = {&HE9}
        Dim DreiNOP As Byte() = {&H90, &H90, &H90}
        ' Array.Copy(detouraddy, 0, dirtdetour, 1, detouraddy.Length)

        '   Dim jumptopatch As Byte() = dirtdetour
        '    Dim test As Integer = (dirtoriginaddress + 8) - injectionaddress - injection.Count - jumptopatch.Count - DreiNOP.Count - dirtdetour.Count
        Dim calculateoriginaddy As Byte() = BitConverter.GetBytes((dirtoriginaddress + 8) - injectionaddress - injection.Count - E9.Count - DreiNOP.Count - E9.Count)


        WriteMemory(injectionaddress, injection)
        WriteMemory(injectionaddress + injection.Count, E9)
        WriteMemory(injectionaddress + injection.Count + E9.Count, calculateoriginaddy)

        WriteMemory(dirtoriginaddress, E9)
        WriteMemory(dirtoriginaddress + E9.Count, detouraddy)
        WriteMemory(dirtoriginaddress + E9.Count + detouraddy.Count, DreiNOP)

    End Sub


    Private Function patternscan(ByVal startindex As Long, ByVal pattern As Byte(), ByVal mask As String, ByVal bytearraytoscan() As Byte) As IntPtr
        Dim maskk(0 To mask.Length - 1) As String
        For i As Integer = 0 To mask.Length - 1
            maskk(i) = mask.Substring(i, 1)
        Next

        For i As Integer = 0 To bytearraytoscan.Length
            Dim n(pattern.Length - 1) As Byte
            Array.Copy(bytearraytoscan, i, n, 0, pattern.Length)
            For k As Integer = 0 To n.Length - 1
                If maskk(k).ToLower = "x" Then
                    If pattern(k) = n(k) Then
                        If k = n.Length - 1 Then
                            Return startindex + i '+ pattern.Length
                        Else
                            Continue For

                        End If
                    Else
                        Exit For
                    End If
                    Continue For
                End If
            Next

        Next
        Return 0

    End Function
End Class
