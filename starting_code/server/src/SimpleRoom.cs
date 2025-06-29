using shared;

namespace server
{
    /**
     * Subclasses Room to create an SimpleRoom which allows adding members without any special considerations.
     */
    abstract class SimpleRoom : Room
    {
        protected SimpleRoom(TCPGameServer pServer) : base(pServer) { }

        public void AddMember(TcpMessageChannel pChannel)
        {
            addMember(pChannel);
        }

        // The HasMember and ForceRemoveMember methods are already available from the base Room class
        // No need to override them here since they're already public in the base class
    }
}