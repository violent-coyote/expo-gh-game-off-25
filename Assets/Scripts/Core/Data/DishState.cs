using System;
using Expo.Data;
using Expo.Core;
using Expo.Core.Events;

namespace Expo.Runtime
{
    public enum DishStatus { NotFired, Cooking, OnPass, Walking, Dead, Served }

    [Serializable]
    public class DishState
    {
        public readonly int DishInstanceId;
        public readonly DishData Data;
        public DishStatus Status { get; private set; }
        public float ElapsedTime { get; private set; }
        public float CookTime { get; private set; }

        private float _fireTime;
        private bool _active;

        public DishState(DishData data, int id)
        {
            Data = data;
            DishInstanceId = id;
            Status = DishStatus.NotFired;
        }

        public void Fire()
        {
            if (Status != DishStatus.NotFired) return;
            Status = DishStatus.Cooking;
            _fireTime = GameTime.Time;
            CookTime = Data.pickupTime;
            _active = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_active) return;

            ElapsedTime += deltaTime;

            if (Status == DishStatus.Cooking && ElapsedTime >= CookTime)
                Ready();
        }

        private void Ready()
        {
            Status = DishStatus.OnPass;
            _active = false;

            EventBus.Publish(new DishReadyEvent
            {
                DishData = Data,
                DishState = this,
                Station = Data.station,
                DishInstanceId = DishInstanceId,
                Timestamp = GameTime.Time,
                CookTime = CookTime
            });
        }
		
		public void MoveToPass()
        {
            Status = DishStatus.OnPass;
            ElapsedTime = 0f;
            _active = false;
        }

        public void IncrementElapsed(float dt)
        {
            ElapsedTime += dt;
        }

        public void MarkWalking()
        {
            if (Status == DishStatus.OnPass)
                Status = DishStatus.Walking;
        }

        public void Kill()
		{
			Status = DishStatus.Dead;
			_active = false;
		}

        public void Serve()
        {
            Status = DishStatus.Served;
            _active = false;
        }
    }
}
