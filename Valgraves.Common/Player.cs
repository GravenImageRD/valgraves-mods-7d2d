namespace Valgraves.Common
{
    /// <summary>
    /// Helpers for working with the local player.
    /// </summary>
    public static class Player
    {
        private static EntityPlayer _currentPlayer = null;

        /// <summary>
        /// Gets the local player if available.
        /// </summary>
        public static EntityPlayer Entity
        {
            get
            {
                if (GameManager.Instance.worldCreated)
                {
                    _currentPlayer = GameManager.Instance.World?.GetPrimaryPlayer();
                    if (_currentPlayer == null)
                    {
                        Log.Warning("Could not find primary player.");
                    }
                }

                return _currentPlayer;
            }
        }
        
        /// <summary>
        /// Gets the entity ID of the local player, or -1 if unavailable.
        /// </summary>
        public static int EntityId => _currentPlayer?.entityId ?? -1;
    }
}