namespace SistemaTesourariaEclesiastica.Enums
{
    /// <summary>
    /// Severidade de uma inconsistência encontrada no sistema
    /// </summary>
    public enum SeveridadeInconsistencia
    {
        /// <summary>
        /// Inconsistência crítica que pode afetar a integridade dos dados financeiros
        /// </summary>
        Critica = 1,

        /// <summary>
        /// Aviso sobre possível problema que deve ser investigado
        /// </summary>
        Aviso = 2,

        /// <summary>
        /// Informação sobre situação atípica mas não necessariamente incorreta
        /// </summary>
        Informacao = 3
    }
}
