// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Tas;
using SAM.Core.Windows.WPF;
using System.Threading;

namespace SAM.Analytical.Revit.UI
{
    public static partial class Modify
    {
        /// <summary>
        /// Runs the TAS workflow with a progress dialog that carries a Cancel button, for the Revit
        /// <see cref="Simulate"/> command. Cancellation is cooperative and between-step (see
        /// <see cref="WorkflowCalculator.CancellationToken"/>): it aborts before the next step but cannot
        /// interrupt the in-flight TAS COM simulate/sizing call.
        /// <para>
        /// The dialog runs on its own UI thread (<see cref="ProgressWindowHost"/>) rather than on Revit's. The
        /// workflow blocks the calling thread for minutes at a time, and Windows ghosts a window whose thread
        /// has stopped pumping and then discards clicks on the ghost — so a Cancel button on Revit's own thread
        /// silently loses the click and the run carries on to completion. The job itself stays on the calling
        /// thread, so no TAS COM object changes apartment and no Revit API call moves off the API thread.
        /// </para>
        /// <para>
        /// This deliberately mirrors <c>SAM.Analytical.Grasshopper.Tas.Modify.RunWorkflow</c> rather than
        /// sharing it: that lives in a Grasshopper assembly, and the only sensible common home would be
        /// <c>SAM.Analytical.Tas</c>, which is kept free of any UI dependency. The part that would actually
        /// drift — the note wording and the list of uninterruptible stages — IS shared, through
        /// <see cref="Query.CancelNote"/>.
        /// </para>
        /// On cancellation the method returns null and sets <paramref name="cancelled"/> true; on any other
        /// failure it returns null with <paramref name="cancelled"/> false.
        /// </summary>
        /// <param name="externalCancellationToken">
        /// Lets the caller share one cancel across its own COM pre-step and this one, so a single Cancel click
        /// aborts either.
        /// </param>
        public static AnalyticalModel RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, CancellationToken externalCancellationToken, out bool cancelled)
        {
            cancelled = false;

            if (analyticalModel == null)
            {
                return null;
            }

            if (workflowSettings == null)
            {
                workflowSettings = new WorkflowSettings();
            }

            AnalyticalModel result = analyticalModel;

            using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken))
            {
                // Not a using: the dialog has to be torn down BEFORE the final cancellation check below, and a
                // using would dispose it after. The Cancel button lives on the dialog's own UI thread, so it can
                // be clicked at any instant - including after a check placed here. Checking then disposing only
                // moves that race; disposing then checking closes it, because Dispose closes the window and
                // joins its thread, so afterwards no further CancelRequested can arrive and any in-flight one
                // has already run.
                //
                // No owner is set on this dialog, unlike every other window this assembly opens over Revit. A
                // WPF owner must live on the same thread as the window it owns, and this one deliberately does
                // not - that is the entire point of the host. ProgressWindowHost sets Topmost instead, which is
                // what keeps it in front of a Revit window that has stopped painting.
                ProgressWindowHost progressWindowHost = new ProgressWindowHost("Tas Workflow", 1, true, Analytical.Tas.Query.CancelNote(null));

                try
                {
                    progressWindowHost.CancelRequested += (s, e) => cancellationTokenSource.Cancel();

                    WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings)
                    {
                        CancellationToken = cancellationTokenSource.Token
                    };

                    workflowCalculator.StepsCounted += (s, e) =>
                    {
                        progressWindowHost.Max = e.Count;
                    };

                    workflowCalculator.Updating += (s, e) =>
                    {
                        progressWindowHost.Note = Analytical.Tas.Query.CancelNote(e.Description);
                        progressWindowHost.Update(e.Description);
                    };

                    result = workflowCalculator.Calculate(analyticalModel);
                }
                catch (System.OperationCanceledException)
                {
                    cancelled = true;
                    result = null;
                }
                finally
                {
                    progressWindowHost.Dispose();
                }

                // Past this point no cancel can be raised, so this observation is final. It catches a click that
                // landed after WorkflowCalculator's own last check - without it Simulate would go on to write
                // results into the Revit document after the user had asked it to stop.
                //
                // "Final" holds only once the host confirms it shut down cleanly. If it could not - the dialog
                // thread was not joined, or a handler did not quiesce - that thread is still live and a click it
                // has queued may never have been observed, so success cannot be claimed and the safe direction
                // is to report the run as cancelled. The expensive artifacts (.tbd/.tsd) are on disk either way;
                // what is given up is only the in-memory handoff, which a rerun reproduces.
                if (!cancelled && (cancellationTokenSource.IsCancellationRequested || !progressWindowHost.ShutdownCompleted))
                {
                    cancelled = true;
                    result = null;
                }
            }

            return result;
        }
    }
}
