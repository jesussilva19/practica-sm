/// Interface for agents that can communicate via FIPA ACL messages
public interface ICommunicationAgent
{
    /// Receives a FIPA ACL message from another agent
    void ReceiveMessage(FipaAclMessage message);
}