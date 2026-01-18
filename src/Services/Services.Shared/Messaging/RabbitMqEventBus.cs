namespace Services.Shared.Messaging;

public static class RabbitMqEventBus
{
    public const string ExchangeName = "innoclinic.events";

    public const string DoctorCreatedRoutingKey = "doctor.created";
    public const string DoctorCreatedQueue = "identity.doctor-created";
}

