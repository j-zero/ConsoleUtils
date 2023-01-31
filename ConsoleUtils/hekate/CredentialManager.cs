﻿// https://stackoverflow.com/a/22435672
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.ComponentModel;

public static class CredentialManager
{
    public static Credential ReadCredential(string applicationName)
    {
        IntPtr nCredPtr;
        bool read = CredRead(applicationName, CredentialType.Generic, 0, out nCredPtr);
        if (read)
        {
            using (CriticalCredentialHandle critCred = new CriticalCredentialHandle(nCredPtr))
            {
                CREDENTIAL cred = critCred.GetCredential();
                return ReadCredential(cred);
            }
        }

        return null;
    }

    private static Credential ReadCredentialString(CREDENTIAL credential)
    {
        string applicationName = Marshal.PtrToStringUni(credential.TargetName);
        string userName = Marshal.PtrToStringUni(credential.UserName);
        string secret = null;
        if (credential.CredentialBlob != IntPtr.Zero)
        {
            secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        }

        return new Credential(credential.Type, applicationName, userName, secret);
    }

    private static Credential ReadCredential(CREDENTIAL credential)
    {
        string applicationName = Marshal.PtrToStringUni(credential.TargetName);
        string userName = Marshal.PtrToStringUni(credential.UserName);
        byte[] secret = new byte[credential.CredentialBlobSize];

        if (credential.CredentialBlob != IntPtr.Zero)
        {
            Marshal.Copy(credential.CredentialBlob, secret, 0, (int)credential.CredentialBlobSize);
            //secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
        }

        return new Credential(credential.Type, applicationName, userName, secret);
    }

    public static int WriteCredential(string applicationName, string userName, byte[] secret, CredentialPersistence credentialPersistence)
    {
        //byte[] byteArray = Encoding.Unicode.GetBytes(secret);
        if (secret.Length > 512)
            throw new ArgumentOutOfRangeException("secret", "The secret message has exceeded 512 bytes.");

        CREDENTIAL credential = new CREDENTIAL();
        credential.AttributeCount = 0;
        credential.Attributes = IntPtr.Zero;
        credential.Comment = IntPtr.Zero;
        credential.TargetAlias = IntPtr.Zero;
        credential.Type = CredentialType.Generic;
        credential.Persist = (UInt32)credentialPersistence;
        //credential.Persist = (UInt32)CredentialPersistence.Enterprise;
        credential.TargetName = Marshal.StringToCoTaskMemUni(applicationName);
        credential.UserName = Marshal.StringToCoTaskMemUni(userName ?? Environment.UserName);

        //credential.CredentialBlob = Marshal.StringToCoTaskMemUni(secret);

        credential.CredentialBlob = Marshal.AllocHGlobal(secret.Length);
        Marshal.Copy(secret, 0, credential.CredentialBlob, secret.Length);
        //Marshal.FreeHGlobal(credential.CredentialBlob);

        credential.CredentialBlobSize = (UInt32)secret.Length;
        //credential.CredentialBlobSize = (UInt32)Encoding.Unicode.GetBytes(secret).Length;

        bool written = CredWrite(ref credential, 0);
        int lastError = Marshal.GetLastWin32Error();

        Marshal.FreeCoTaskMem(credential.TargetName);
        //Marshal.FreeCoTaskMem(credential.CredentialBlob);
        Marshal.FreeHGlobal(credential.CredentialBlob);
        Marshal.FreeCoTaskMem(credential.UserName);

        if (written)
            return 0;

        throw new Exception(string.Format("CredWrite failed with the error code {0}.", lastError));
    }

    public static IReadOnlyList<Credential> EnumerateCrendentials()
    {
        List<Credential> result = new List<Credential>();

        int count;
        IntPtr pCredentials;
        bool ret = CredEnumerate(null, 0, out count, out pCredentials);
        if (ret)
        {
            for (int n = 0; n < count; n++)
            {
                IntPtr credential = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(IntPtr)));
                result.Add(ReadCredential((CREDENTIAL)Marshal.PtrToStructure(credential, typeof(CREDENTIAL))));
            }
        }
        else
        {
            int lastError = Marshal.GetLastWin32Error();
            throw new Win32Exception(lastError);
        }

        return result;
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] UInt32 flags);

    [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CredEnumerate(string filter, int flag, out int count, out IntPtr pCredentials);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    static extern bool CredFree([In] IntPtr cred);



    public enum CredentialPersistence : uint
    {
        Session = 1,
        LocalMachine,
        Enterprise
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public UInt32 Flags;
        public CredentialType Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public UInt32 CredentialBlobSize;
        public IntPtr CredentialBlob;
        public UInt32 Persist;
        public UInt32 AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        public CriticalCredentialHandle(IntPtr preexistingHandle)
        {
            SetHandle(preexistingHandle);
        }

        public CREDENTIAL GetCredential()
        {
            if (!IsInvalid)
            {
                CREDENTIAL credential = (CREDENTIAL)Marshal.PtrToStructure(handle, typeof(CREDENTIAL));
                return credential;
            }

            throw new InvalidOperationException("Invalid CriticalHandle!");
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
            {
                CredFree(handle);
                SetHandleAsInvalid();
                return true;
            }

            return false;
        }
    }
}

public enum CredentialType
{
    Generic = 1,
    DomainPassword,
    DomainCertificate,
    DomainVisiblePassword,
    GenericCertificate,
    DomainExtended,
    Maximum,
    MaximumEx = Maximum + 1000,
}

public class Credential
{
    private readonly string _applicationName;
    private readonly string _userName;
    private readonly string _password;
    private readonly byte[] _passwordRaw;
    private readonly CredentialType _credentialType;

    public CredentialType CredentialType
    {
        get { return _credentialType; }
    }

    public string ApplicationName
    {
        get { return _applicationName; }
    }

    public string UserName
    {
        get { return _userName; }
    }

    public string Password
    {
        get { return _password; }
    }
    public byte[] RawPassword
    {
        get { return _passwordRaw; }
    }

    public Credential(CredentialType credentialType, string applicationName, string userName, string password)
    {
        _applicationName = applicationName;
        _userName = userName;
        _password = password;
        _credentialType = credentialType;
    }
    public Credential(CredentialType credentialType, string applicationName, string userName, byte[] password)
    {
        _applicationName = applicationName;
        _userName = userName;
        _password = Encoding.Unicode.GetString(password);
        _passwordRaw = password;
        _credentialType = credentialType;
    }

    public override string ToString()
    {
        return string.Format("CredentialType: {0}, ApplicationName: {1}, UserName: {2}, Password: {3}", CredentialType, ApplicationName, UserName, Password);
    }
}