using System;
using System.Collections.Generic;
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

        [SerializeField] private int tubeIndex;
        [SerializeField] private float selectedOffset = 0.25f;
        [SerializeField] private float selectedScale = 1.05f;
        [SerializeField] private float moveDuration = 0.2f;

        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private bool _isSelected;

        private Stack<TubeColor> _colorsStack;
        private List<Ball> _balls = new();

        public int Count => _balls.Count;
        public bool IsEmpty => _balls.Count == 0;
        public bool IsFull => _balls.Count >= ballSlots.Length;
        public TubeColor TopColor => _balls[_balls.Count - 1].Color;

        private void Start()
        {
            SpawnBalls();
        }

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
            int count = 0;
            for (int i = _balls.Count - 1; i >= 0; --i)
            {
                if (_balls[i].Color != top) break;
                count++;
            }

            return count;
        }

// Trả về số bóng có thể đổ sang target (0 = không đổ được)
        public int GetPourAmount(Tube target)
        {
            if (target == this || IsEmpty || target.IsFull) return 0;
            if (!target.IsEmpty && target.TopColor != TopColor) return 0;

            int freeSpace = target.ballSlots.Length - target.Count;
            return Mathf.Min(GetTopSameColorCount(), freeSpace);
        }

// Đổ `amount` bóng sang target, trả về Sequence để biết khi nào xong
        public Sequence PourTo(Tube target, int amount)
        {
            Sequence sequence = DOTween.Sequence();

            // Điểm bóng bay lên trên miệng ống nguồn (tính 1 lần lúc bắt đầu)
            Vector3 exitPoint = ballSlots[ballSlots.Length - 1].position + Vector3.up * exitOffset;

            for (int i = 0; i < amount; ++i)
            {
                // Cập nhật dữ liệu ngay lập tức, animation chạy sau
                Ball ball = _balls[_balls.Count - 1];
                _balls.RemoveAt(_balls.Count - 1);

                int targetSlotIndex = target._balls.Count;
                Transform targetSlot = target.ballSlots[targetSlotIndex];
                target._balls.Add(ball);

                ball.transform.SetParent(null, true); // tách khỏi slot cũ để di chuyển tự do

                // Bay lên -> bay đến slot đích -> gắn vào slot
                sequence.Append(ball.transform.DOMove(exitPoint, ballMoveDuration).SetEase(Ease.OutQuad));
                sequence.Append(ball.transform.DOMove(targetSlot.position, ballMoveDuration).SetEase(Ease.InQuad));
                sequence.AppendCallback(() =>
                {
                    ball.transform.SetParent(targetSlot);
                    ball.transform.position = targetSlot.position;
                    ball.SetSprite(target.GetSprite(ball.Color, targetSlotIndex == 0));
                });
            }

            return sequence;
        }

        private void SpawnBalls()
        {
            TubeColor[] colors = _colorsStack.ToArray();
            Array.Reverse(colors);

            for (int i = 0; i < colors.Length; ++i)
            {
                bool isBottom = (i == 0);
                Ball ball = Instantiate(ballPrefab, ballSlots[i].position, Quaternion.identity, ballSlots[i]);
                ball.Setup(colors[i], GetSprite(colors[i], isBottom));
                _balls.Add(ball);
            }
        }

        private Sprite GetSprite(TubeColor color, bool isBottom)
        {
            switch (color)
            {
                case TubeColor.Red: return isBottom ? redBottomSprite : redSprite;
                case TubeColor.Blue: return isBottom ? blueBottomSprite : blueSprite;
                case TubeColor.Green: return isBottom ? greenBottomSprite : greenSprite;
                default: return null;
            }
        }

        private void OnMouseDown()
        {
            FindObjectOfType<GameManager>().OnTubeClicked(this);
        }
    }
}

public enum TubeColor
{
    Red,
    Blue,
    Green
}