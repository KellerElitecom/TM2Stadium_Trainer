Public Class Form1
    Dim grasaddy As Integer
    Dim grasbase As Integer
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

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim jmptopatchbytes As Byte()
        Dim bigendian As Integer = &H400310 - 5 - grasaddy
        jmptopatchbytes = BitConverter.GetBytes(bigendian)
        'Array.Reverse(jmptopatchbytes)
        Dim E9JMP As Byte() = {&HE9}
        Dim DreiNOP As Byte() = {&H90, &H90, &H90}
         Dim dirtpatch As Byte() = {&H66, &H8B, &H47, &HC, &H66, &H3D, &H2, &H0, &HF, &H84, &HA, &H0, &H0, &H0, &H66, &H3D, &H6, &H0, &HF, &H85, &H4, &H0, &H0, &H0, &H66, &HB8, &HA, &H0, &H66, &H89, &H46, &H78, &HE9}
        Dim myint As Integer = grasaddy + E9JMP.Count + 5 - grasbase - 310 - dirtpatch.Count - 1 - DreiNOP.Count
        'FEHLER
        Dim jumpbacktoorigin As Byte() = BitConverter.GetBytes(myint)
        '(64c412+8 + e9jmp.coun) - (400000 - (310 + 32)) 13 bytes fehlen?
        WriteMemory(grasbase + &H310, dirtpatch)
        WriteMemory(grasbase + &H310 + dirtpatch.Count, jumpbacktoorigin)

        WriteMemory(grasaddy, E9JMP)
        WriteMemory(grasaddy + E9JMP.Count, jmptopatchbytes)
        WriteMemory(grasaddy + E9JMP.Count + jmptopatchbytes.Count, DreiNOP)


    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        grasbase = &H400000 'GetModuleInfo("maniaplanet.exe", False).ToInt32
        'Dim granddump As Byte() = ReadMemory(Of Byte())(GetModuleInfo("maniaplanet.exe", False), GetModuleInfo("maniaplanet.exe", True), False)
        ' Dim mypattern As Byte() = {&H66, &H8B, &H47, &HC, &H66, &H89, &H46, &H78, &H8B, &H47}
        grasaddy = &H64C412 ' patternscan(0, mypattern, "xxx?xxx?xx", granddump).ToInt32
        'grasaddy += grasbase - mypattern.Count

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
                            Return startindex + i + pattern.Length
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

    Private Sub Button4_Click(sender As Object, e As EventArgs)

    End Sub
End Class
