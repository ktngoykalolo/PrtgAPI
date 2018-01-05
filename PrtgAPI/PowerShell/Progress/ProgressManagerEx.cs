﻿using PrtgAPI.PowerShell.Base;

namespace PrtgAPI.PowerShell.Progress
{
    /// <summary>
    /// Extended progress state that must be saved between <see cref="ProgressManager"/> teardowns with each call to ProcessRecord
    /// </summary>
    class ProgressManagerEx
    {
        internal Pipeline BlockingSelectPipeline { get; set; }

        /// <summary>
        /// The last record that was written to before a <see cref="PrtgMultiOperationCmdlet"/> entered its EndProcessing method.
        /// </summary>
        internal ProgressRecordEx CachedRecord { get; set; }
    }
}
