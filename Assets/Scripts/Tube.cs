using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace DefaultNamespace
{
    public class Tube : MonoBehaviour
    {
        [Header("Balls")] [SerializeField] private Ball ballPrefab;
        [SerializeField] private Transform[] ballSlots;
        [SerializeField] private Sprite redSprite;
        [SerializeField] private Sprite blueSprite;
        [SerializeField] private Sprite greenSprite;

        [Header("Bottom Sprites")] [SerializeField]
        private Sprite redBottomSprite;

        [SerializeField] private Sprite blueBottomSprite;
        [SerializeField] private Sprite greenBottomSprite;

        [Header("Pour")] [SerializeField] private float exitOffset = 0.8f;
        [SerializeField] private float ballMoveDuration = 0.15f;
        [SerializeField] private int pouringSortingOrderOffset = 100;

        [SerializeField] private int tubeIndex;
        [SerializeField] private float selectedOffset = 0.25f;
        [SerializeField] private float selectedScale = 1.05f;
        [SerializeField] private float moveDuration = 0.2f;

        private float _tubeOffset = 0.05f;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _isSelected;

        private Stack<TubeColor> _colorsStack;
        private List<Ball> _balls = new();

        private int Count => _balls.Count;
        public bool IsEmpty => _balls.Count == 0;
        private bool IsFull => _balls.Count >= ballSlots.Length;
        private TubeColor TopColor => _balls.Last().Color;

        private void Start() => SpawnBalls();

        private void Awake()
        {
            _originalPosition = transform.position;
            _originalScale = transform.localScale;
            _colorsStack = new Stack<TubeColor>();

            var index = transform.GetSiblingIndex();
            tubeIndex = index;

            switch (tubeIndex)
            {
                case 0:
                    _colorsStack.Push(TubeColor.Red);
                    _colorsStack.Push(TubeColor.Green);
                    _colorsStack.Push(TubeColor.Blue);
                    break;
                case 1:
                    _colorsStack.Push(TubeColor.Green);
                    _colorsStack.Push(TubeColor.Blue);
                    _colorsStack.Push(TubeColor.Red);
                    break;
            }
        }

        public void Select()
        {
            if (_isSelected) return;
            _isSelected = true;

            transform.DOKill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMoveY(_originalPosition.y + selectedOffset, moveDuration).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_originalScale * selectedScale, moveDuration).SetEase(Ease.OutQuad));
        }

        public void Deselect()
        {
            if (!_isSelected) return;
            _isSelected = false;

            transform.DOKill();
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMoveY(_originalPosition.y, moveDuration).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_originalScale, moveDuration).SetEase(Ease.OutQuad));
        }


        private int GetTopSameColorCount()
        {
            if (IsEmpty) return 0;

            var top = TopColor;
            var count = 0;
            for (var i = _balls.Count - 1; i >= 0; --i)
            {
                if (_balls[i].Color != top) break;
                count++;
            }

            return count;
        }

        public int GetPourAmount(Tube target)
        {
            if (target == this || IsEmpty || target.IsFull) return 0;

            var freeSpace = target.ballSlots.Length - target.Count;
            return Mathf.Min(GetTopSameColorCount(), freeSpace);
        }

        public Sequence PourTo(Tube target, int amount)
        {
            var sequence = DOTween.Sequence();
            var transfers =
                new List<(Ball ball, Transform targetSlot, int targetSlotIndex, Vector3 originalLocalScale)>();
            var originalBallSortingOrders = _balls
                .Select(ball => (ball, sortingOrder: ball.SortingOrder))
                .ToList();

            var tubeRenderer = GetComponent<SpriteRenderer>();
            var originalTubeSortingOrder = tubeRenderer.sortingOrder;
            tubeRenderer.sortingOrder += pouringSortingOrderOffset;
            foreach (var (ball, _) in originalBallSortingOrders)
            {
                ball.SortingOrder += pouringSortingOrderOffset + 1;
            }

            for (var i = 0; i < amount; ++i)
            {
                var ball = _balls.Last();
                _balls.RemoveAt(_balls.Count - 1);

                var targetSlotIndex = target._balls.Count;
                var targetSlot = target.ballSlots[targetSlotIndex];
                var originalLocalScale = ball.transform.localScale;
                target._balls.Add(ball);
                transfers.Add((ball, targetSlot, targetSlotIndex, originalLocalScale));
            }

            var targetPosition = target.transform.position;
            var distance = _originalPosition.x - targetPosition.x;
            distance += distance > 0 ? -exitOffset : exitOffset;

            sequence
                .Append(transform.DOMoveY(_originalPosition.y + 0.3f, 0.15f))
                .Append(transform.DOMoveX(_originalPosition.x - distance, 0.5f))
                .Append(transform.DORotate(new Vector3(0, 0, distance < 0 ? -15f : 15f), 0.5f))
                .AppendCallback(() =>
                {
                    foreach (var transfer in transfers)
                    {
                        transfer.ball.transform.SetParent(transfer.targetSlot);
                        transfer.ball.transform.position = transfer.targetSlot.position;
                        transfer.ball.SetSprite(target.GetSprite(transfer.ball.Color, transfer.targetSlotIndex == 0));
                        transfer.ball.transform.rotation = Quaternion.identity;
                        transfer.ball.transform.DOScale(transfer.originalLocalScale, ballMoveDuration)
                            .SetEase(Ease.OutQuad);
                    }
                })
                .Append(transform.DORotate(Vector3.zero, 0.3f))
                .Append(transform.DOMove(_originalPosition, 0.3f))
                .AppendCallback(() =>
                {
                    tubeRenderer.sortingOrder = originalTubeSortingOrder;
                    foreach (var (ball, sortingOrder) in originalBallSortingOrders)
                    {
                        ball.SortingOrder = sortingOrder;
                    }
                });

            return sequence;
        }

        private void SpawnBalls()
        {
            var colors = _colorsStack.ToArray();
            Array.Reverse(colors);

            for (var i = 0; i < colors.Length; ++i)
            {
                var isBottom = (i == 0);
                var ball = Instantiate(ballPrefab, ballSlots[i].position, Quaternion.identity, ballSlots[i]);
                ball.Setup(colors[i], GetSprite(colors[i], isBottom));
                _balls.Add(ball);
            }
        }

        private Sprite GetSprite(TubeColor color, bool isBottom)
        {
            return color switch
            {
                TubeColor.Red => isBottom ? redBottomSprite : redSprite,
                TubeColor.Blue => isBottom ? blueBottomSprite : blueSprite,
                TubeColor.Green => isBottom ? greenBottomSprite : greenSprite,
                _ => null
            };
        }

        public bool CanPourInto(Tube tube) =>
            this._balls.Count > 0 && tube._balls.Count < tube.ballSlots.Length &&
            (tube._balls.Count == 0 || tube.TopColor == this.TopColor);

        private void OnMouseDown() => FindObjectOfType<GameManager>().OnTubeClicked(this);
    }
}

public enum TubeColor
{
    Red,
    Blue,
    Green
}