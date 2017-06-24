﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using PrtgAPI.Helpers;

namespace PrtgAPI.PowerShell
{
    internal class ProgressManager : IDisposable
    {
        private static Stack<ProgressRecord> progressRecords = new Stack<ProgressRecord>();

        internal const string DefaultActivity = "Activity";
        internal const string DefaultDescription = "Description";

        private const string SkipActivity = "SkipActivity";
        private const string SkipDescription = "SkipDescription";

        public ProgressRecord CurrentRecord => progressRecords.Peek();

        public bool ContainsProgress => CurrentRecord.Activity != DefaultActivity && (CurrentRecord.StatusDescription != DefaultDescription || InitialDescription != string.Empty);

        public bool PreviousContainsProgress => PreviousRecord?.Activity != DefaultActivity && PreviousRecord?.StatusDescription != DefaultDescription;

        public bool FirstInChain => pipeToPrtgCmdlet && progressRecords.Count == 1;

        public bool PartOfChain => pipeToPrtgCmdlet || progressRecords.Count > 1;

        private bool pipeToPrtgCmdlet => cmdlet.MyInvocation.MyCommand.ModuleName == cmdlet.CommandRuntime.GetDownstreamCmdlet()?.ModuleName;

        public bool LastInChain => !pipeToPrtgCmdlet;

        public string InitialDescription { get; set; }

        public int? TotalRecords { get; set; }

        public Pipeline Pipeline { get; set; }

        public bool PipeFromVariable => Pipeline?.List.Count() > 1;

        public ProgressRecord PreviousRecord => progressRecords.Skip(1).FirstOrDefault();

        private PSCmdlet cmdlet;

        private int recordsProcessed = -1;

        bool variableProgressDisplayed = false;

        private IProgressWriter progressWriter;

        internal static IProgressWriter CustomWriter { get; set; }

        public ProgressManager(PSCmdlet cmdlet)
        {
            progressRecords.Push(new ProgressRecord(progressRecords.Count + 1, "Activity", "Description"));

            if (PreviousRecord != null)
                CurrentRecord.ParentActivityId = PreviousRecord.ActivityId;

            this.cmdlet = cmdlet;
            Pipeline = cmdlet.CommandRuntime.GetPipelineInput(cmdlet);

            progressWriter = GetWriter();
        }

        private IProgressWriter GetWriter()
        {
            if (CustomWriter != null)
                return CustomWriter;
            else
                return new ProgressWriter(cmdlet);
        }

        ~ProgressManager()
        {
            Dispose(false);
        }

        #region IDisposable

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                progressRecords.Pop();
            }

            disposed = true;
        }

        #endregion

        public void RemovePreviousOperation()
        {
            if (PreviousRecord != null && PreviousRecord.CurrentOperation != null)
            {
                PreviousRecord.CurrentOperation = null;

                WriteProgress(PreviousRecord);
            }
        }

        private void WriteProgress()
        {
            WriteProgress(CurrentRecord);
        }

        public void WriteProgress(string activity, string statusDescription)
        {
            CurrentRecord.Activity = activity;
            CurrentRecord.StatusDescription = statusDescription;

            WriteProgress();
        }

        private void WriteProgress(ProgressRecord progressRecord)
        {
            if (progressRecord.Activity == DefaultActivity || progressRecord.StatusDescription == DefaultDescription)
                throw new InvalidOperationException("Attempted to write progress on an uninitialized ProgressRecord. If this is a Release build, please report this bug along with the cmdlet chain you tried to execute. To disable PrtgAPI Cmdlet Progress in the meantime use Disable-PrtgProgress");

            if (PreviousRecord == null)
                progressWriter.WriteProgress(progressRecord);
            else
            {
                var sourceId = GetLastSourceId(cmdlet.CommandRuntime);

                progressWriter.WriteProgress(sourceId, progressRecord);
            }
        }

        internal static long GetLastSourceId(ICommandRuntime commandRuntime)
        {
            return Convert.ToInt64(commandRuntime.GetType().GetField("_lastUsedSourceId", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
        }

        public void CompleteProgress()
        {
            if (PipeFromVariable && Pipeline.CurrentIndex < Pipeline.List.Count - 1)
                return;

            InitialDescription = null;
            recordsProcessed = -1;

            if (TotalRecords > 0)
            {
                CurrentRecord.RecordType = ProgressRecordType.Completed;

                WriteProgress();
            }

            TotalRecords = null;

            CurrentRecord.Activity = DefaultActivity;
            CurrentRecord.StatusDescription = DefaultDescription;
            CurrentRecord.RecordType = ProgressRecordType.Processing;
        }

        public void UpdateRecordsProcessed()
        {
            if (PipeFromVariable)
            {
                var index = variableProgressDisplayed ? Pipeline.CurrentIndex + 2 : Pipeline.CurrentIndex + 1;

                if (index > Pipeline.List.Count)
                    index = Pipeline.List.Count;

                CurrentRecord.StatusDescription = $"{InitialDescription} {index}/{Pipeline.List.Count}";

                CurrentRecord.PercentComplete = (int) ((index) /Convert.ToDouble(Pipeline.List.Count)*100);

                variableProgressDisplayed = true;

                WriteProgress();
             }
            else
            {
                if (TotalRecords > 0)
                {
                    recordsProcessed++;

                    CurrentRecord.StatusDescription = $"{InitialDescription} {recordsProcessed}/{TotalRecords}";

                    if (recordsProcessed > 0)
                        CurrentRecord.PercentComplete = (int)(recordsProcessed / Convert.ToDouble(TotalRecords) * 100);

                    WriteProgress();
                }
            }
        }

        public void TrySetPreviousOperation(string operation)
        {
            if (PreviousRecord != null)
                SetPreviousOperation(operation);
        }

        public void SetPreviousOperation(string operation)
        {
            PreviousRecord.CurrentOperation = operation;

            WriteProgress(PreviousRecord);
        }

        public void DisplayInitialProgress()
        {
            if (PipeFromVariable && Pipeline.CurrentIndex > 0)
                return;

            CurrentRecord.StatusDescription = InitialDescription;

            WriteProgress();
        }

        public void TryOverwritePreviousOperation(string activity, string progressMessage)
        {
            //i think we need to revert the way we're doing this. say processing sensors, then if we detect we're going to pipe to someone display some progress that counts through our items
            //i think we already use this logic for "normal" pipes, right?

            if (PreviousRecord != null)
            {
                //if we're piping from a variable, that means the current count is actually a count of the variable; therefore, we need to inspect the pipeline to get the number of triggers incoming?              

                PreviousRecord.Activity = activity;

                var count = PreviousRecord.StatusDescription.Substring(PreviousRecord.StatusDescription.LastIndexOf(" ") + 1);

                PreviousRecord.StatusDescription = $"{progressMessage} ({count.Trim('(').Trim(')')})";

                WriteProgress(PreviousRecord);

                SkipCurrentRecord();
            }
            else
            {
                if (PipeFromVariable)
                {
                    var index = variableProgressDisplayed ? Pipeline.CurrentIndex + 2 : Pipeline.CurrentIndex + 1;

                    if (index > Pipeline.List.Count)
                        index = Pipeline.List.Count;

                    CurrentRecord.Activity = activity;
                    CurrentRecord.PercentComplete = (int)((index) / Convert.ToDouble(Pipeline.List.Count) * 100);
                    CurrentRecord.StatusDescription = $"{progressMessage} ({index}/{Pipeline.List.Count})";

                    variableProgressDisplayed = true;

                    WriteProgress();
                }
            }
        }

        private void SkipCurrentRecord()
        {
            CloneRecord(PreviousRecord, CurrentRecord);
        }

        public static ProgressRecord CloneRecord(ProgressRecord progressRecord)
        {
            var record = new ProgressRecord(progressRecord.ActivityId, progressRecord.Activity, progressRecord.StatusDescription)
            {
                CurrentOperation = progressRecord.CurrentOperation,
                ParentActivityId = progressRecord.ParentActivityId,
                PercentComplete = progressRecord.PercentComplete,
                RecordType = progressRecord.RecordType,
                SecondsRemaining = progressRecord.SecondsRemaining
            };

            return record;
        }

        public static void CloneRecord(ProgressRecord sourceRecord, ProgressRecord destinationRecord)
        {
            destinationRecord.GetType().GetField("id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(destinationRecord, sourceRecord.ActivityId);
            destinationRecord.Activity = sourceRecord.Activity;
            destinationRecord.StatusDescription = sourceRecord.StatusDescription;
            destinationRecord.CurrentOperation = sourceRecord.CurrentOperation;
            destinationRecord.ParentActivityId = sourceRecord.ParentActivityId;
            destinationRecord.PercentComplete = sourceRecord.PercentComplete;
            destinationRecord.RecordType = sourceRecord.RecordType;
            destinationRecord.SecondsRemaining = sourceRecord.SecondsRemaining;
        }
    }
}
