using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Cadenza
{
    public class CrowdBehavior : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Sprite handsDownSprite;
        [SerializeField] private Sprite handsUpSprite;
        private enum CrowdAction
        {
            DoNothing,
            Sway,
            Spin,
            Jump,
        }

        private float actionCooldownTimer;
        private bool isHoldingPlayer;
        private bool isInAction;
        private List<Collider> heldEntities = new List<Collider>();

        void Awake()
        {
            DOTween.Init();
            this.actionCooldownTimer = 0f;
            this.isHoldingPlayer = false;
            this.isInAction = false;
            GameObject parentObject = this.playerTransform.parent != null ? this.playerTransform.parent.gameObject : null;
            if (parentObject != null && parentObject.GetComponent<OverallCrowd>() != null)
            {
                Debug.Log("CrowdBehavior: Found OverallCrowd component, setting sprites.");
                this.handsDownSprite = parentObject.GetComponent<OverallCrowd>().HandsDownSprite;
                this.handsUpSprite = parentObject.GetComponent<OverallCrowd>().HandsUpSprite;
            }
        }

        void Update()
        {
            if (this.isInAction || this.isHoldingPlayer) return;
            if (this.actionCooldownTimer > 0f)
            {
                this.actionCooldownTimer -= Time.deltaTime;
                return;
            }
            
            this.DoRandomAction();
        }

        private void DoRandomAction()
        {
            this.isInAction = true;
            int actionIndex = Random.Range(0, System.Enum.GetValues(typeof(CrowdAction)).Length);
            switch ((CrowdAction)actionIndex)
            {
                case CrowdAction.Sway:
                    this.spriteRenderer.sprite = this.handsDownSprite;
                    this.playerTransform.DORotate(new Vector3(0, 15, 0), 0.5f).SetLoops(4, LoopType.Yoyo).OnComplete(() =>
                    {
                        this.ConcludeAction();
                    });
                    break;
                case CrowdAction.Spin:
                    this.spriteRenderer.sprite = this.handsDownSprite;
                    this.playerTransform.DORotate(new Vector3(0, 360, 0), 1f, RotateMode.FastBeyond360).OnComplete(() =>
                    {
                        this.ConcludeAction();
                    });
                    break;
                case CrowdAction.Jump:
                    this.spriteRenderer.sprite = this.handsUpSprite;
                    this.playerTransform.DOJump(this.playerTransform.position, 1f, 1, 0.5f).SetLoops(4, LoopType.Yoyo).OnComplete(() =>
                    {
                        this.ConcludeAction();
                    });
                    break;
                default:
                    this.spriteRenderer.sprite = this.handsDownSprite;
                    this.ConcludeAction();
                    break;
            }
        }

        private void ConcludeAction()
        {
            this.isInAction = false;
            this.actionCooldownTimer = Random.Range(1f, 5f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("Enemy"))
            {
                this.isHoldingPlayer = true;    
                this.heldEntities.Add(other);
                this.spriteRenderer.sprite = this.handsUpSprite;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.CompareTag("Enemy"))
            {
                this.heldEntities.Remove(other);
                if (this.heldEntities.Count == 0)
                {
                    this.isHoldingPlayer = false;
                    this.spriteRenderer.sprite = this.handsDownSprite;
                }
            }
        }
    }
}