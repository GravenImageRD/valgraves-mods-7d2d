namespace Valgraves.Common
{
    public static class Player
    {
        private static EntityPlayer _currentPlayer = null;

        public static EntityPlayer Entity
        {
            get
            {
                _currentPlayer = GameManager.Instance.World?.GetPrimaryPlayer();
                if (_currentPlayer == null)
                {
                    //Log.Error("Could not find primary player!");
                }
                return _currentPlayer;
            }
        }
        
        public static int EntityId => _currentPlayer?.entityId ?? -1;
    }
}