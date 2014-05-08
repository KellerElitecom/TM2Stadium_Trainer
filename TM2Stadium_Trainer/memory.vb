Imports System.Runtime.InteropServices
Imports System.Text

Module memory

    <DllImport("kernel32.dll")> _
    Private Function OpenProcess(ByVal dwDesiredAccess As UInteger, <MarshalAs(UnmanagedType.Bool)> ByVal bInheritHandle As Boolean, ByVal dwProcessId As Integer) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)> _
    Private Function WriteProcessMemory(ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer As Byte(), ByVal nSize As IntPtr, <Out()> ByRef lpNumberOfBytesWritten As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)> _
    Private Function ReadProcessMemory(ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, <Out()> ByVal lpBuffer() As Byte, ByVal dwSize As IntPtr, ByRef lpNumberOfBytesRead As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)> _
    Private Function CloseHandle(ByVal hObject As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("kernel32", CharSet:=CharSet.Auto, SetLastError:=True)> _
    Public Function VirtualProtectEx(ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, _
    ByVal dwSize As IntPtr, ByVal flNewProtect As UInteger, _
    ByRef lpflOldProtect As UInteger) As Boolean
    End Function
    <DllImport("kernel32.dll", SetLastError:=True, ExactSpelling:=True)> _
    Public Function VirtualAllocEx(ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, _
     ByVal dwSize As UInteger, ByVal flAllocationType As UInteger, _
     ByVal flProtect As UInteger) As IntPtr
    End Function
    'Declare Function VirtualProtectEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As IntPtr, ByVal newProtect As Integer, ByRef oldProtect As Integer) As Boolean
    ' Declare Function VirtualAllocEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As IntPtr, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As IntPtr



    Private Const PROCESS_VM_WRITE As UInteger = &H20
    Private Const PROCESS_VM_READ As UInteger = &H10
    Private Const PROCESS_VM_OPERATION As UInteger = &H8
    ' Private Const TargetProcess As String = "8923njer82834sssfe42gt6"
    Public TargetProcess As String = "maniaplanet"
    Private ProcessHandle As IntPtr = IntPtr.Zero
    Private LastKnownPID As Integer = -1

    Sub RemoveProtection(ByVal AddressOfStart As Integer, ByVal SizeToRemoveProtectionInBytes As Integer)
        For Each p As Process In Process.GetProcessesByName(TargetProcess)
            Const PAGE_EXECUTE_READWRITE As Integer = &H40
            Dim oldProtect As Integer
            If Not VirtualProtectEx(p.Handle, New IntPtr(AddressOfStart), New IntPtr(SizeToRemoveProtectionInBytes), PAGE_EXECUTE_READWRITE, oldProtect) Then Throw New Exception
            p.Dispose()
        Next
    End Sub

    Sub AllocMem(ByVal ProcessName As String, ByVal AddressOfStart As Integer, ByVal SizeOfAllocationInBytes As Integer)
        For Each p As Process In Process.GetProcessesByName(ProcessName)
            Const MEM_COMMIT As Integer = &H1000
            Const PAGE_EXECUTE_READWRITE As Integer = &H40
            Dim pBlob As IntPtr = VirtualAllocEx(p.Handle, New IntPtr(AddressOfStart), New IntPtr(SizeOfAllocationInBytes), MEM_COMMIT, PAGE_EXECUTE_READWRITE)
            If pBlob = IntPtr.Zero Then Throw New Exception
            p.Dispose()
        Next
    End Sub
    Public Function ReadMemory(Of T)(ByVal address As Long) As T
        Return ReadMemory(Of T)(address, 0, False)
    End Function

    Public Function ReadMemory(ByVal address As Long, ByVal length As Integer) As Byte()
        Return ReadMemory(Of Byte())(address, length, False)
    End Function

    Private Function ProcessIDExists(ByVal pID As Integer) As Boolean
        For Each p As Process In Process.GetProcessesByName(TargetProcess)

            If p.Id = pID Then Return True
        Next
        Return False
    End Function

    Public Function UpdateProcessHandle() As Boolean
        Try

            If LastKnownPID = -1 OrElse Not ProcessIDExists(LastKnownPID) Then
                If ProcessHandle <> IntPtr.Zero Then CloseHandle(ProcessHandle)
                Dim p() As Process = Process.GetProcessesByName(TargetProcess)
                If p.Length = 0 Then Return False
                LastKnownPID = p(0).Id
                ProcessHandle = OpenProcess(PROCESS_VM_READ Or PROCESS_VM_WRITE Or PROCESS_VM_OPERATION, False, p(0).Id)
                If ProcessHandle = IntPtr.Zero Then Return False
            End If

            Return True

        Catch ex As Exception
            Return False
        End Try
    End Function


    Public Function ReadMemory(Of T)(ByVal address As Long, ByVal length As Integer, ByVal unicodeString As Boolean) As T
        Dim buffer() As Byte
        If GetType(T) Is GetType(String) Then
            If unicodeString Then buffer = New Byte(length * 2 - 1) {} Else buffer = New Byte(length - 1) {}
        ElseIf GetType(T) Is GetType(Byte()) Then
            buffer = New Byte(length - 1) {}
        Else
            buffer = New Byte(Marshal.SizeOf(GetType(T)) - 1) {}
        End If
        If Not UpdateProcessHandle() Then Return Nothing
        Dim success As Boolean = ReadProcessMemory(ProcessHandle, New IntPtr(address), buffer, New IntPtr(buffer.Length), IntPtr.Zero)
        If Not success Then Return Nothing
        If GetType(T) Is GetType(Byte()) Then Return CType(CType(buffer, Object), T)
        Dim gcHandle As GCHandle = gcHandle.Alloc(buffer, GCHandleType.Pinned)
        Dim returnObject As T
        returnObject = CType(Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject, GetType(T)), T)
        gcHandle.Free()
        Return returnObject
    End Function

    Private Function GetObjectBytes(ByVal value As Object) As Byte()
        If value.GetType() Is GetType(Byte()) Then Return CType(value, Byte())
        Dim buffer(Marshal.SizeOf(value) - 1) As Byte
        Dim ptr As IntPtr = Marshal.AllocHGlobal(buffer.Length)
        Marshal.StructureToPtr(value, ptr, True)
        Marshal.Copy(ptr, buffer, 0, buffer.Length)
        Marshal.FreeHGlobal(ptr)
        Return buffer
    End Function

    Public Function WriteMemory(ByVal address As Long, ByVal value As Object) As Boolean
        Return WriteMemory(address, value, False)
    End Function

    Public Function WriteMemory(ByVal address As Long, ByVal value As Object, ByVal unicode As Boolean, Optional ByVal size As Integer = 0) As Boolean
        If Not UpdateProcessHandle() Then Return False
        Dim buffer() As Byte
        Dim result As Boolean
        If TypeOf value Is String Then
            If unicode Then buffer = Encoding.Unicode.GetBytes(value.ToString()) Else buffer = Encoding.ASCII.GetBytes(value.ToString())
        Else
            buffer = GetObjectBytes(value)
        End If
        If size = 0 Then
            result = WriteProcessMemory(ProcessHandle, New IntPtr(address), buffer, New IntPtr(buffer.Length), IntPtr.Zero)
        Else
            result = WriteProcessMemory(ProcessHandle, New IntPtr(address), buffer, New IntPtr(size), IntPtr.Zero)
        End If

        Return result
    End Function

    Public Function GetBaseAddress(ByVal MyProcess As String) As Integer
        Dim p As Process() = Process.GetProcessesByName(MyProcess)
        Dim pID As IntPtr = p(0).Handle
        Dim base As IntPtr = p(0).MainModule.BaseAddress
        Return CInt(base)
    End Function
    Public Function FindMyAddress(ByVal moduleName As String, _
                                  ByVal StaticPointer As IntPtr, ByVal Offsets() As String) As IntPtr

        Dim Address As IntPtr
        Dim tmp(IntPtr.Size - 1) As Byte

        Try
            Dim running As Process() = Process.GetProcessesByName(TargetProcess)
            If running.Length > 0 Then
                Dim target As Process = running(0)
                Dim targetModule As ProcessModule = (From pm In target.Modules _
                                                     Where pm.ModuleName.ToLower().Equals(moduleName.ToLower()) _
                                                     Select pm).FirstOrDefault()
                If targetModule IsNot Nothing Then
                    Address = targetModule.BaseAddress

                    If IntPtr.Size = 4 Then
                        Address = New IntPtr(Address.ToInt32 + StaticPointer.ToInt32)
                    Else
                        Address = New IntPtr(Address.ToInt64 + StaticPointer.ToInt64)
                    End If
                    If Not Offsets(0) = "none" Then
                        For i As Integer = 0 To Offsets.Length - 1
                            ReadProcessMemory(running(0).Handle, Address, tmp, IntPtr.Size, 0)
                            If IntPtr.Size = 4 Then
                                Dim i32 As Int32 = Int(Offsets(i))
                                Address = BitConverter.ToInt32(tmp, 0) + i32
                            Else
                                Dim i64 As Int64 = Int(Offsets(i))
                                Address = BitConverter.ToInt64(tmp, 0) + i64
                            End If
                        Next
                    End If


                    Return Address
                End If
            Else
                Return IntPtr.Zero ' Throw New ArgumentOutOfRangeException("Target process is not running")
            End If

        Catch ex As Exception
            ' MessageBox.Show(TargetProcess.ToString & " is not running!")
        End Try
        Return IntPtr.Zero
    End Function

    Public Function GetModuleInfo(ByVal modul As String, ByVal returnsize As Boolean) As IntPtr
        Dim Address As IntPtr
        Dim tmp(IntPtr.Size - 1) As Byte

        Try
            Dim running As Process() = Process.GetProcessesByName(TargetProcess)
            If running.Length > 0 Then
                Dim target As Process = running(0)
                Dim targetModule As ProcessModule = (From pm In target.Modules _
                                                     Where pm.ModuleName.ToLower().Equals(modul.ToLower()) _
                                                     Select pm).FirstOrDefault()
                If targetModule IsNot Nothing Then
                    If returnsize Then
                        Address = targetModule.ModuleMemorySize
                    Else
                        Address = targetModule.BaseAddress
                    End If



                    If IntPtr.Size = 4 Then
                        Address = New IntPtr(Address.ToInt32)
                    Else
                        Address = New IntPtr(Address.ToInt64)
                    End If
                    'If Not Offsets(0) = "none" Then
                    '    For i As Integer = 0 To Offsets.Length - 1
                    '        ReadProcessMemory(running(0).Handle, Address, tmp, IntPtr.Size, 0)
                    '        If IntPtr.Size = 4 Then
                    '            Dim i32 As Int32 = Int(Offsets(i))
                    '            Address = BitConverter.ToInt32(tmp, 0) + i32
                    '        Else
                    '            Dim i64 As Int64 = Int(Offsets(i))
                    '            Address = BitConverter.ToInt64(tmp, 0) + i64
                    '        End If
                    '    Next
                    'End If


                    Return Address
                End If
            Else
                Return IntPtr.Zero ' Throw New ArgumentOutOfRangeException("Target process is not running")
            End If

        Catch ex As Exception
            ' MessageBox.Show(TargetProcess.ToString & " is not running!")
        End Try
        Return IntPtr.Zero
    End Function

End Module
