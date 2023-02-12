namespace Portfolio.Framework
{
    public abstract class EntityProcessor<EntityType> : Processor where EntityType : IEntity, new()
    {
        private readonly EntityType _entity;
        protected EntityType Entity => _entity;
        public EntityProcessor(EntityType entity)
        {
            _entity = entity;
        }
    }
}

