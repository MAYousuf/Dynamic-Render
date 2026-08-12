namespace TechnicalInspection.PoC.Requests;

public enum InspectionRequestStatus
{
    Draft = 0,
    Submitted = 1
}

/// <summary>
/// Lifecycle of the Step 3 data for a single inspection (section 12 of the design).
/// <c>Invalidated</c> is what a Step 2 change produces when it makes previously entered
/// Step 3 data meaningless.
/// </summary>
public enum InspectionDataStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Invalidated = 3
}
