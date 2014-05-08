Public Class Form1

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
End Class
