using Obilet.Application.Models;

namespace Obilet.Application.Abstractions;

/// <summary>
/// Device Session gerektiren API çağrılarını, oturum yönetimini çağrı
/// noktalarından gizleyerek yürütür.
/// </summary>
/// <remarks>
/// Her servisin "oturumu al, çağır, oturum geçersizse yenile ve bir kez
/// tekrar dene" kalıbını elle yazması gerekmesin diye vardır. Bu kalıp tek
/// bir yerde yaşar ve tek bir yerde test edilir.
/// </remarks>
public interface IObiletCallExecutor
{
    /// <summary>
    /// Verilen çağrıyı geçerli Device Session ile yürütür. API oturumu
    /// geçersiz sayarsa oturumu yeniler ve çağrıyı <b>bir kez</b> tekrarlar.
    /// </summary>
    Task<TResult> ExecuteAsync<TResult>(
        Func<DeviceSession, CancellationToken, Task<TResult>> call,
        CancellationToken cancellationToken = default);
}
