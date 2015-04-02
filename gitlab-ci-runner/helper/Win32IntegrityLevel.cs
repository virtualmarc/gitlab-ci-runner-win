﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace gitlab_ci_runner.helper {
    public class Win32IntegrityLevel {
        /// from https://code.msdn.microsoft.com/windowsapps/CSCreateLowIntegrityProcess-d7cb5e4d/sourcecode?fileId=52580&pathId=1953311234
        #region Helper Functions related to Process Integrity Level
        /// <summary>
        /// The function launches an application at low integrity level.
        /// </summary>
        /// <param name="commandLine">
        /// The command line to be executed. The maximum length of this string is 32K characters.
        /// </param>
        /// <remarks>
        /// To start a low-integrity process,
        /// 1) Duplicate the handle of the current process, which is at medium integrity level.
        /// 2) Use SetTokenInformation to set the integrity level in the access token to Low.
        /// 3) Use CreateProcessAsUser to create a new process using the handle to the low integrity access token.
        /// </remarks>
        public int CreateIntegrityProcess(SECURITY_MANDATORY_RID IL, string commandLine) {
            return this.CreateIntegrityProcess((int)IL, commandLine);
        }
        public int CreateIntegrityProcess(Int32 IntegrityLevel, string commandLine) {
            int pid;
            SafeTokenHandle hToken = null;
            SafeTokenHandle hNewToken = null;
            IntPtr pIntegritySid = IntPtr.Zero;
            int cbTokenInfo = 0;
            IntPtr pTokenInfo = IntPtr.Zero;
            STARTUPINFO si = new STARTUPINFO();
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

            try {
                // Open the primary access token of the process.
                if (!NativeMethod.OpenProcessToken(Process.GetCurrentProcess().Handle,
                    NativeMethod.TOKEN_DUPLICATE | NativeMethod.TOKEN_ADJUST_DEFAULT |
                    NativeMethod.TOKEN_QUERY | NativeMethod.TOKEN_ASSIGN_PRIMARY,
                    out hToken)) {
                    throw new Win32Exception();
                }

                // Duplicate the primary token of the current process.
                if (!NativeMethod.DuplicateTokenEx(hToken, 0, IntPtr.Zero,
                    SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary, out hNewToken)) {
                    throw new Win32Exception();
                }

                // Create the integrity SID.
                if (!NativeMethod.AllocateAndInitializeSid(
                    ref NativeMethod.SECURITY_MANDATORY_LABEL_AUTHORITY, 1,
                    IntegrityLevel,
                    0, 0, 0, 0, 0, 0, 0, out pIntegritySid)) {
                    throw new Win32Exception();
                }

                TOKEN_MANDATORY_LABEL tml;
                tml.Label.Attributes = NativeMethod.SE_GROUP_INTEGRITY;
                tml.Label.Sid = pIntegritySid;

                // Marshal the TOKEN_MANDATORY_LABEL struct to the native memory.
                cbTokenInfo = Marshal.SizeOf(tml);
                pTokenInfo = Marshal.AllocHGlobal(cbTokenInfo);
                Marshal.StructureToPtr(tml, pTokenInfo, false);

                // Set the integrity level in the access token to low.
                if (!NativeMethod.SetTokenInformation(hNewToken,
                    TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, pTokenInfo,
                    cbTokenInfo + NativeMethod.GetLengthSid(pIntegritySid))) {
                    throw new Win32Exception();
                }

                // Create the new process at the Low integrity level.
                si.cb = Marshal.SizeOf(si);
                if (!NativeMethod.CreateProcessAsUser(hNewToken, null, commandLine,
                    IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si,
                    out pi)) {
                    throw new Win32Exception();
                }
                pid = pi.dwProcessId;
            } finally {
                // Centralized cleanup for all allocated resources.
                if (hToken != null) {
                    hToken.Close();
                    hToken = null;
                }
                if (hNewToken != null) {
                    hNewToken.Close();
                    hNewToken = null;
                }
                if (pIntegritySid != IntPtr.Zero) {
                    NativeMethod.FreeSid(pIntegritySid);
                    pIntegritySid = IntPtr.Zero;
                }
                if (pTokenInfo != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pTokenInfo);
                    pTokenInfo = IntPtr.Zero;
                    cbTokenInfo = 0;
                }
                if (pi.hProcess != IntPtr.Zero) {
                    NativeMethod.CloseHandle(pi.hProcess);
                    pi.hProcess = IntPtr.Zero;
                }
                if (pi.hThread != IntPtr.Zero) {
                    NativeMethod.CloseHandle(pi.hThread);
                    pi.hThread = IntPtr.Zero;
                }
            }
            return pid;
        }


        /// <summary>
        /// The function gets the integrity level of the current process.
        /// Integrity level is only available on Windows Vista and newer operating systems,
        /// thus GetProcessIntegrityLevel throws a C++ exception if it is called on systems prior to Windows Vista.
        /// </summary>
        /// <returns>
        /// Returns the integrity level of the current process. It is usually one of these values:
        ///
        ///    SECURITY_MANDATORY_UNTRUSTED_RID - means untrusted level.
        ///    It is used by processes started by the Anonymous group. Blocks most write access.
        ///    (SID: S-1-16-0x0)
        ///
        ///    SECURITY_MANDATORY_LOW_RID - means low integrity level.
        ///    It is used by Protected Mode Internet Explorer.
        ///    Blocks write acess to most objects (such as files and registry keys) on the system.
        ///    (SID: S-1-16-0x1000)
        ///
        ///    SECURITY_MANDATORY_MEDIUM_RID - means medium integrity level.
        ///    It is used by normal applications being launched while UAC is enabled.
        ///    (SID: S-1-16-0x2000)
        ///
        ///    SECURITY_MANDATORY_HIGH_RID - means high integrity level.
        ///    It is used by administrative applications launched through elevation when UAC is
        ///    enabled, or normal applications if UAC is disabled and the user is an administrator.
        ///    (SID: S-1-16-0x3000)
        ///
        ///    SECURITY_MANDATORY_SYSTEM_RID - means system integrity level.
        ///    It is used by services and other system-level applications (such as Wininit, Winlogon, Smss, etc.)
        ///    (SID: S-1-16-0x4000)
        ///
        /// </returns>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// When any native Windows API call fails, the function throws a Win32Exception with the last error code.
        /// </exception>
        public int GetProcessIntegrityLevel() {
            int IL = -1;
            SafeTokenHandle hToken = null;
            int cbTokenIL = 0;
            IntPtr pTokenIL = IntPtr.Zero;

            try {
                // Open the access token of the current process with TOKEN_QUERY.
                if (!NativeMethod.OpenProcessToken(Process.GetCurrentProcess().Handle,
                    NativeMethod.TOKEN_QUERY, out hToken)) {
                    throw new Win32Exception();
                }

                // Then we must query the size of the integrity level information associated with the token.
                // Note that we expect GetTokenInformation to return false with the ERROR_INSUFFICIENT_BUFFER error code
                // because we've given it a null buffer. On exit cbTokenIL will tell the size of the group information.
                if (!NativeMethod.GetTokenInformation(hToken,
                    TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, IntPtr.Zero, 0,
                    out cbTokenIL)) {
                    int error = Marshal.GetLastWin32Error();
                    if (error != NativeMethod.ERROR_INSUFFICIENT_BUFFER) {
                        // When the process is run on operating systems prior to Windows Vista,
                        // GetTokenInformation returns false with the ERROR_INVALID_PARAMETER error code because
                        // TokenIntegrityLevel is not supported on those OS's.
                        throw new Win32Exception(error);
                    }
                }

                // Now we allocate a buffer for the integrity level information.
                pTokenIL = Marshal.AllocHGlobal(cbTokenIL);
                if (pTokenIL == IntPtr.Zero) {
                    throw new Win32Exception();
                }

                // Now we ask for the integrity level information again. This may fail
                // if an administrator has added this account to an additional group
                // between our first call to GetTokenInformation and this one.
                if (!NativeMethod.GetTokenInformation(hToken,
                    TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, pTokenIL, cbTokenIL,
                    out cbTokenIL)) {
                    throw new Win32Exception();
                }

                // Marshal the TOKEN_MANDATORY_LABEL struct from native to .NET object.
                TOKEN_MANDATORY_LABEL tokenIL = (TOKEN_MANDATORY_LABEL)
                    Marshal.PtrToStructure(pTokenIL, typeof(TOKEN_MANDATORY_LABEL));

                // Integrity Level SIDs are in the form of S-1-16-0xXXXX.
                // (e.g. S-1-16-0x1000 stands for low integrity level SID).
                // There is one and only one subauthority.
                IntPtr pIL = NativeMethod.GetSidSubAuthority(tokenIL.Label.Sid, 0);
                IL = Marshal.ReadInt32(pIL);
            } finally {
                // Centralized cleanup for all allocated resources.
                if (hToken != null) {
                    hToken.Close();
                    hToken = null;
                }
                if (pTokenIL != IntPtr.Zero) {
                    Marshal.FreeHGlobal(pTokenIL);
                    pTokenIL = IntPtr.Zero;
                    cbTokenIL = 0;
                }
            }

            return IL;
        }

        public SECURITY_MANDATORY_RID GetCurrentIntegrityLevel() {
            return (SECURITY_MANDATORY_RID)this.GetProcessIntegrityLevel();
        }
        #endregion

        /// from https://code.msdn.microsoft.com/windowsapps/CSCreateLowIntegrityProcess-d7cb5e4d/sourcecode?fileId=52580&pathId=6398360
        #region NativeMethod
        /// <summary>
        /// The TOKEN_INFORMATION_CLASS enumeration type contains values that specify
        /// the type of information being assigned to or retrieved from an access token.
        /// </summary>
        internal enum TOKEN_INFORMATION_CLASS {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /// <summary>
        /// The SECURITY_IMPERSONATION_LEVEL enumeration type contains values that specify security impersonation levels.
        /// Security impersonation levels govern the degree to which a server process can act on behalf of a client process.
        /// </summary>
        internal enum SECURITY_IMPERSONATION_LEVEL {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        /// <summary>
        /// The TOKEN_TYPE enumeration type contains values that differentiate between a primary token and an impersonation token.
        /// </summary>
        internal enum TOKEN_TYPE {
            TokenPrimary = 1,
            TokenImpersonation
        }

        /// <summary>
        /// The structure represents a security identifier (SID) and its attributes. SIDs are used to uniquely identify users or groups.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SID_AND_ATTRIBUTES {
            public IntPtr Sid;
            public UInt32 Attributes;
        }

        /// <summary>
        /// The SID_IDENTIFIER_AUTHORITY structure represents the top-level authority of a security identifier (SID).
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SID_IDENTIFIER_AUTHORITY {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6,
                ArraySubType = UnmanagedType.I1)]
            public byte[] Value;

            public SID_IDENTIFIER_AUTHORITY(byte[] value) {
                this.Value = value;
            }
        }

        /// <summary>
        /// The structure specifies the mandatory integrity level for a token.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_MANDATORY_LABEL {
            public SID_AND_ATTRIBUTES Label;
        }

        /// <summary>
        /// Specifies the window station, desktop, standard handles, and appearance of the main window for a process at creation time.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct STARTUPINFO {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        /// <summary>
        /// Contains information about a newly created process and its primary thread.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        /// <summary>
        /// Represents a wrapper class for a token handle.
        /// </summary>
        internal class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid {
            private SafeTokenHandle()
                : base(true) {
            }

            internal SafeTokenHandle(IntPtr handle)
                : base(true) {
                base.SetHandle(handle);
            }

            protected override bool ReleaseHandle() {
                return NativeMethod.CloseHandle(base.handle);
            }
        }

        public enum SECURITY_MANDATORY_RID {
            Untrusted = NativeMethod.SECURITY_MANDATORY_UNTRUSTED_RID,
            Low = NativeMethod.SECURITY_MANDATORY_LOW_RID,
            Medium = NativeMethod.SECURITY_MANDATORY_MEDIUM_RID,
            High = NativeMethod.SECURITY_MANDATORY_HIGH_RID,
            System = NativeMethod.SECURITY_MANDATORY_SYSTEM_RID
        }

        internal class NativeMethod {
            // Token Specific Access Rights

            public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
            public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
            public const UInt32 TOKEN_DUPLICATE = 0x0002;
            public const UInt32 TOKEN_IMPERSONATE = 0x0004;
            public const UInt32 TOKEN_QUERY = 0x0008;
            public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
            public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
            public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
            public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
            public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
            public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
            public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED |
                TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE |
                TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES |
                TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);


            public const Int32 ERROR_INSUFFICIENT_BUFFER = 122;


            // Integrity Levels

            public static SID_IDENTIFIER_AUTHORITY SECURITY_MANDATORY_LABEL_AUTHORITY =
                new SID_IDENTIFIER_AUTHORITY(new byte[] { 0, 0, 0, 0, 0, 16 });
            public const Int32 SECURITY_MANDATORY_UNTRUSTED_RID = 0x00000000;
            public const Int32 SECURITY_MANDATORY_LOW_RID = 0x00001000;
            public const Int32 SECURITY_MANDATORY_MEDIUM_RID = 0x00002000;
            public const Int32 SECURITY_MANDATORY_HIGH_RID = 0x00003000;
            public const Int32 SECURITY_MANDATORY_SYSTEM_RID = 0x00004000;


            // Group related SID Attributes

            public const UInt32 SE_GROUP_MANDATORY = 0x00000001;
            public const UInt32 SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
            public const UInt32 SE_GROUP_ENABLED = 0x00000004;
            public const UInt32 SE_GROUP_OWNER = 0x00000008;
            public const UInt32 SE_GROUP_USE_FOR_DENY_ONLY = 0x00000010;
            public const UInt32 SE_GROUP_INTEGRITY = 0x00000020;
            public const UInt32 SE_GROUP_INTEGRITY_ENABLED = 0x00000040;
            public const UInt32 SE_GROUP_LOGON_ID = 0xC0000000;
            public const UInt32 SE_GROUP_RESOURCE = 0x20000000;
            public const UInt32 SE_GROUP_VALID_ATTRIBUTES = (SE_GROUP_MANDATORY |
                SE_GROUP_ENABLED_BY_DEFAULT | SE_GROUP_ENABLED | SE_GROUP_OWNER |
                SE_GROUP_USE_FOR_DENY_ONLY | SE_GROUP_LOGON_ID | SE_GROUP_RESOURCE |
                SE_GROUP_INTEGRITY | SE_GROUP_INTEGRITY_ENABLED);


            /// <summary>
            /// The function opens the access token associated with a process.
            /// </summary>
            /// <param name="hProcess">
            /// A handle to the process whose access token is opened.
            /// </param>
            /// <param name="desiredAccess">
            /// Specifies an access mask that specifies the requested types of access to the access token.
            /// </param>
            /// <param name="hToken">
            /// Outputs a handle that identifies the newly opened access token when the function returns.
            /// </param>
            /// <returns></returns>
            [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(
                IntPtr hProcess,
                UInt32 desiredAccess,
                out SafeTokenHandle hToken);


            /// <summary>
            /// The DuplicateTokenEx function creates a new access token that duplicates an existing token.
            /// This function can create either a primary token or an impersonation token.
            /// </summary>
            /// <param name="hExistingToken">
            /// A handle to an access token opened with TOKEN_DUPLICATE access.
            /// </param>
            /// <param name="desiredAccess">
            /// Specifies the requested access rights for the new token.
            /// </param>
            /// <param name="pTokenAttributes">
            /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor
            /// for the new token and determines whether child processes can inherit the token.
            /// If lpTokenAttributes is NULL, the token gets a default security descriptor and
            /// the handle cannot be inherited.
            /// </param>
            /// <param name="ImpersonationLevel">
            /// Specifies the impersonation level of the new token.
            /// </param>
            /// <param name="TokenType">
            /// TokenPrimary - The new token is a primary token that you can use in the CreateProcessAsUser function.
            /// TokenImpersonation - The new token is an impersonation token.
            /// </param>
            /// <param name="hNewToken">
            /// Receives the new token.
            /// </param>
            /// <returns></returns>
            [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DuplicateTokenEx(
                SafeTokenHandle hExistingToken,
                UInt32 desiredAccess,
                IntPtr pTokenAttributes,
                SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
                TOKEN_TYPE TokenType,
                out SafeTokenHandle hNewToken);


            /// <summary>
            /// The function retrieves a specified type of information about an access token.
            /// The calling process must have appropriate access rights to obtain the information.
            /// </summary>
            /// <param name="hToken">
            /// A handle to an access token from which information is retrieved.
            /// </param>
            /// <param name="tokenInfoClass">
            /// Specifies a value from the TOKEN_INFORMATION_CLASS enumerated type to identify the type of information the function retrieves.
            /// </param>
            /// <param name="pTokenInfo">
            /// A pointer to a buffer the function fills with the requested information.
            /// </param>
            /// <param name="tokenInfoLength">
            /// Specifies the size, in bytes, of the buffer pointed to by the TokenInformation parameter.
            /// </param>
            /// <param name="returnLength">
            /// A pointer to a variable that receives the number of bytes needed for the buffer pointed to by the TokenInformation parameter.
            /// </param>
            /// <returns></returns>
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetTokenInformation(
                SafeTokenHandle hToken,
                TOKEN_INFORMATION_CLASS tokenInfoClass,
                IntPtr pTokenInfo,
                Int32 tokenInfoLength,
                out Int32 returnLength);


            /// <summary>
            /// The function sets various types of information for a specified access token.
            /// The information that this function sets replaces existing information.
            /// The calling process must have appropriate access rights to set the information.
            /// </summary>
            /// <param name="hToken">
            /// A handle to the access token for which information is to be set.
            /// </param>
            /// <param name="tokenInfoClass">
            /// A value from the TOKEN_INFORMATION_CLASS enumerated type that identifies the type of information the function sets.
            /// </param>
            /// <param name="pTokenInfo">
            /// A pointer to a buffer that contains the information set in the access token.
            /// </param>
            /// <param name="tokenInfoLength">
            /// Specifies the length, in bytes, of the buffer pointed to by TokenInformation.
            /// </param>
            /// <returns></returns>
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetTokenInformation(
                SafeTokenHandle hToken,
                TOKEN_INFORMATION_CLASS tokenInfoClass,
                IntPtr pTokenInfo,
                Int32 tokenInfoLength);


            /// <summary>
            /// The function returns a pointer to a specified subauthority in a security identifier (SID).
            /// The subauthority value is a relative identifier (RID).
            /// </summary>
            /// <param name="pSid">
            /// A pointer to the SID structure from which a pointer to a subauthority is to be returned.
            /// </param>
            /// <param name="nSubAuthority">
            /// Specifies an index value identifying the subauthority array element whose address the function will return.
            /// </param>
            /// <returns>
            /// If the function succeeds, the return value is a pointer to the specified SID subauthority.
            /// To get extended error information, call GetLastError.
            /// If the function fails, the return value is undefined.
            /// The function fails if the specified SID structure is not valid or if the index value specified
            /// by the nSubAuthority parameter is out of bounds.
            /// </returns>
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetSidSubAuthority(
                IntPtr pSid,
                UInt32 nSubAuthority);


            /// <summary>
            /// The AllocateAndInitializeSid function allocates and initializes a security identifier (SID) with up to eight subauthorities.
            /// </summary>
            /// <param name="pIdentifierAuthority">
            /// A reference of a SID_IDENTIFIER_AUTHORITY structure.
            /// This structure provides the top-level identifier authority value to set in the SID.
            /// </param>
            /// <param name="nSubAuthorityCount">
            /// Specifies the number of subauthorities to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority0">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority1">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority2">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority3">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority4">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority5">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority6">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="dwSubAuthority7">
            /// Subauthority value to place in the SID.
            /// </param>
            /// <param name="pSid">
            /// Outputs the allocated and initialized SID structure.
            /// </param>
            /// <returns>
            /// If the function succeeds, the return value is true.
            /// If the function fails, the return value is false.
            /// To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AllocateAndInitializeSid(
                ref SID_IDENTIFIER_AUTHORITY pIdentifierAuthority,
                byte nSubAuthorityCount,
                int dwSubAuthority0, int dwSubAuthority1,
                int dwSubAuthority2, int dwSubAuthority3,
                int dwSubAuthority4, int dwSubAuthority5,
                int dwSubAuthority6, int dwSubAuthority7,
                out IntPtr pSid);


            /// <summary>
            /// The FreeSid function frees a security identifier (SID) previously allocated by using the AllocateAndInitializeSid function.
            /// </summary>
            /// <param name="pSid">
            /// A pointer to the SID structure to free.
            /// </param>
            /// <returns>
            /// If the function succeeds, the function returns NULL.
            /// If the function fails, it returns a pointer to the SID structure represented by the pSid parameter.
            /// </returns>
            [DllImport("advapi32.dll")]
            public static extern IntPtr FreeSid(IntPtr pSid);


            /// <summary>
            /// The function returns the length, in bytes, of a valid security identifier (SID).
            /// </summary>
            /// <param name="pSID">
            /// A pointer to the SID structure whose length is returned.
            /// </param>
            /// <returns>
            /// If the SID structure is valid, the return value is the length, in bytes, of the SID structure.
            /// </returns>
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetLengthSid(IntPtr pSID);


            /// <summary>
            /// Creates a new process and its primary thread.
            /// The new process runs in the security context of the user represented by the specified token.
            /// </summary>
            /// <param name="hToken">
            /// A handle to the primary token that represents a user.
            /// </param>
            /// <param name="applicationName">
            /// The name of the module to be executed.
            /// </param>
            /// <param name="commandLine">
            /// The command line to be executed. The maximum length of this string is 32K characters.
            /// </param>
            /// <param name="pProcessAttributes">
            /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor
            /// for the new process object and determines whether child processes can inherit the
            /// returned handle to the process.
            /// </param>
            /// <param name="pThreadAttributes">
            /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor
            /// for the new thread object and determines whether child processes can inherit the
            /// returned handle to the thread.
            /// </param>
            /// <param name="bInheritHandles">
            /// If this parameter is true, each inheritable handle in the calling process is inherited by the new process.
            /// If the parameter is false, the handles are not inherited.
            /// </param>
            /// <param name="dwCreationFlags">
            /// The flags that control the priority class and the creation of the process.
            /// </param>
            /// <param name="pEnvironment">
            /// A pointer to an environment block for the new process.
            /// </param>
            /// <param name="currentDirectory">
            /// The full path to the current directory for the process.
            /// </param>
            /// <param name="startupInfo">
            /// References a STARTUPINFO structure.
            /// </param>
            /// <param name="processInformation">
            /// Outputs a PROCESS_INFORMATION structure that receives identification information about the new process.
            /// </param>
            /// <returns></returns>
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CreateProcessAsUser(
                SafeTokenHandle hToken,
                string applicationName,
                string commandLine,
                IntPtr pProcessAttributes,
                IntPtr pThreadAttributes,
                bool bInheritHandles,
                uint dwCreationFlags,
                IntPtr pEnvironment,
                string currentDirectory,
                ref STARTUPINFO startupInfo,
                out PROCESS_INFORMATION processInformation);


            /// <summary>
            /// Closes an open object handle.
            /// </summary>
            /// <param name="handle">A valid handle to an open object.</param>
            /// <returns></returns>
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr handle);
        }

        /// <summary>
        /// Well-known folder paths
        /// </summary>
        internal class KnownFolder {
            private static readonly Guid LocalAppDataGuid = new Guid(
                "F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
            public static string LocalAppData {
                get { return SHGetKnownFolderPath(LocalAppDataGuid); }
            }

            private static readonly Guid LocalAppDataLowGuid = new Guid(
                "A520A1A4-1780-4FF6-BD18-167343C5AF16");
            public static string LocalAppDataLow {
                get { return SHGetKnownFolderPath(LocalAppDataLowGuid); }
            }


            /// <summary>
            /// Retrieves the full path of a known folder identified by the folder's KNOWNFOLDERID.
            /// </summary>
            /// <param name="rfid">
            /// A reference to the KNOWNFOLDERID that identifies the folder.
            /// </param>
            /// <returns></returns>
            public static string SHGetKnownFolderPath(Guid rfid) {
                IntPtr pPath = IntPtr.Zero;
                string path = null;
                try {
                    int hr = SHGetKnownFolderPath(rfid, 0, IntPtr.Zero, out pPath);
                    if (hr != 0) {
                        throw Marshal.GetExceptionForHR(hr);
                    }
                    path = Marshal.PtrToStringUni(pPath);
                } finally {
                    if (pPath != IntPtr.Zero) {
                        Marshal.FreeCoTaskMem(pPath);
                        pPath = IntPtr.Zero;
                    }
                }
                return path;
            }


            /// <summary>
            /// Retrieves the full path of a known folder identified by the folder's KNOWNFOLDERID.
            /// </summary>
            /// <param name="rfid">
            /// A reference to the KNOWNFOLDERID that identifies the folder.
            /// </param>
            /// <param name="dwFlags">
            /// Flags that specify special retrieval options.
            /// </param>
            /// <param name="hToken">
            /// An access token that represents a particular user.
            /// If this parameter is NULL, which is the most common usage, the function requests the known folder for the current user.
            /// </param>
            /// <param name="pszPath">
            /// When this method returns, contains the address of a pointer to a
            /// null-terminated Unicode string that specifies the path of the known folder.
            /// The calling process is responsible for freeing this
            /// resource once it is no longer needed by calling CoTaskMemFree.
            /// </param>
            /// <returns>HRESULT</returns>
            [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                uint dwFlags,
                IntPtr hToken,
                out IntPtr pszPath);
        }
        #endregion
    }
}
