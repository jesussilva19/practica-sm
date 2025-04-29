/// <summary>
/// Interface for agents that can communicate via FIPA ACL messages
/// </summary>
public interface ICommunicationAgent
{
    /// <summary>
    /// Receives a FIPA ACL message from another agent
    /// </summary>
    /// <param name="message">The message to be received</param>
    void ReceiveMessage(FipaAclMessage message);
}