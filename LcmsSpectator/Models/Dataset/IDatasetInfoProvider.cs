namespace LcmsSpectator.Models.Dataset
{
    /// <summary>
    /// Interface to classes that get information about datasets.
    /// </summary>
    public interface IDatasetInfoProvider
    {
        /// <summary>
        /// Gets dataset info for the dataset chosen with the provider.
        /// </summary>
        /// <returns>The <see cref="DatasetInfo" />.</returns>
        DatasetInfo GetDatasetInfo();
    }
}
