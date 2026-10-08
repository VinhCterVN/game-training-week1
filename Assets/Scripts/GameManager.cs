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

            bool canPour = _selectedTube.CanPourInto(tube);
            if (canPour)
            {
                _isBusy = true;
                var pourAmount = _selectedTube.GetPourAmount(tube);
                var sequence = _selectedTube.PourTo(tube, pourAmount);
                sequence.OnComplete(() =>
                {
                    _isBusy = false;
                    _selectedTube = null;
                });
                _selectedTube.Deselect();
            }
            else
            {
                _selectedTube.Deselect();
                _selectedTube = tube;
                _selectedTube.Select();
            }
        }
    }
}