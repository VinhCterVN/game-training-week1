using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {
        private Tube _selectedTube;
        private bool _isBusy;

        public void OnTubeClicked(Tube tube)
        {
            if (_isBusy) return;

            if (_selectedTube == null)
            {
                if (tube.IsEmpty) return;
                _selectedTube = tube;
                _selectedTube.Select();
                return;
            }

            if (_selectedTube == tube)
            {
                _selectedTube.Deselect();
                _selectedTube = null;
                return;
            }

            int amount = _selectedTube.GetPourAmount(tube);
            if (amount <= 0)
            {
                _selectedTube.Deselect();
                _selectedTube = null;
                return;
            }

            _isBusy = true;
            Tube from = _selectedTube;
            _selectedTube = null;

            from.PourTo(tube, amount).OnComplete(() =>
            {
                from.Deselect();
                _isBusy = false;
            });
        }
    }
}