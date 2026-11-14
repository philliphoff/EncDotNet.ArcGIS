using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EncDotNet.SoundCharts.Server;

/// <summary>
/// A Windows job object that ends its processes when its last handle
/// closes: when Pro exits, or crashes, the chart servers go with it (design
/// brief §4.2, D18).
/// </summary>
/// <remarks>
/// The add-in keeps one job for the life of Pro's process and never closes
/// the handle itself; Windows closes it when the process ends.
/// </remarks>
public static class KillOnCloseJob
{
    private static readonly Lazy<SafeFileHandle?> Job = new(CreateJob);

    /// <summary>
    /// Puts <paramref name="process"/> in the job.
    /// </summary>
    /// <param name="process">A process this process started.</param>
    /// <returns>Whether it was added; <see langword="false"/> when the job couldn't be created or joined.</returns>
    public static bool TryAssign(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        if (!OperatingSystem.IsWindows() || Job.Value is not { IsInvalid: false } job)
        {
            return false;
        }

        try
        {
            return NativeMethods.AssignProcessToJobObject(job, process.SafeHandle);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            // The process has already exited.
            return false;
        }
    }

    private static SafeFileHandle? CreateJob()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var job = NativeMethods.CreateJobObject(IntPtr.Zero, null);
        if (job.IsInvalid)
        {
            job.Dispose();
            return null;
        }

        var info = new NativeMethods.JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new NativeMethods.JobObjectBasicLimitInformation
            {
                LimitFlags = NativeMethods.JobObjectLimitKillOnJobClose,
            },
        };
        if (!NativeMethods.SetInformationJobObject(
            job,
            NativeMethods.JobObjectExtendedLimitInformationClass,
            ref info,
            (uint)Marshal.SizeOf<NativeMethods.JobObjectExtendedLimitInformation>()))
        {
            job.Dispose();
            return null;
        }

        return job;
    }
}
