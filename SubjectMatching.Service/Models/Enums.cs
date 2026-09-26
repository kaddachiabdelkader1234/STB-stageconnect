namespace SubjectMatching.Service.Models;

public enum SubjectDifficulty
{
    Beginner,
    Intermediate,
    Advanced
}

public enum SubjectStatus
{
    Draft,
    Open,
    Full,
    Assigned,
    Closed
}

public enum AssignmentStatus
{
    Proposed,
    Accepted,
    ChangeRequested,
    Reassigned,
    Rejected
}

public enum ChangeRequestStatus
{
    Pending,
    Approved,
    Rejected
}
