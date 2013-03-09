BEGIN TRANSACTION;

DECLARE @LastPayloadId bigint,
		@NewPayloadId bigint;

SELECT @LastPayloadId = [PayloadId]
FROM [SignalR].[PayloadId];

SET @NewPayloadId = CASE @LastPayloadId WHEN NULL THEN 1 ELSE @LastPayloadId + 1 END;

INSERT INTO [SignalR].[Messages_1] ([PayloadId], [Payload], [InsertedOn]) VALUES (@NewPayloadId ,@Payload, GETDATE());

IF @LastPayloadID IS NULL
	INSERT INTO [SignalR].[PayloadId] ([PayloadId]) VALUES (@NewPayloadId);
ELSE
	UPDATE [SignalR].[PayloadId] SET [PayloadID] = @NewPayloadId;

COMMIT TRANSACTION;