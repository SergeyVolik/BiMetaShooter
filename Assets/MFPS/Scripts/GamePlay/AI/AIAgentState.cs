public enum AIAgentState
{
    Idle = 0,
    Patroling = 1,
    Following = 2,
    Covering = 3,
    Looking = 4,
    Searching = 5,
}

public enum AIAgentBehave
{
    Agressive = 0,
    Pasive = 1,
    Protective = 2,
}

public enum AIAgentCoverArea
{
    ToPoint = 0,
    ToTarget = 1,
    ToRandomPoint = 2,
}