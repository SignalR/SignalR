-- Params: @Payload varbinary(max)
-- Replace: [SignalR] => [schema_name], [Messages_1 => [table_prefix_index

BEGIN TRANSACTION;

DECLARE @LastPayloadId bigint,
		@NewPayloadId bigint;

-- Get last payload id
SELECT @LastPayloadId = [PayloadId]
FROM [SignalR].[Messages_1_Id];

-- Increment payload id
SET @NewPayloadId = COALESCE(@LastPayloadId + 1, 1);

-- Insert payload
INSERT INTO [SignalR].[Messages_1] ([PayloadId], [Payload], [InsertedOn])
VALUES (@NewPayloadId, @Payload, GETDATE());

-- Update last payload id
IF @LastPayloadID IS NULL
	INSERT INTO [SignalR].[Messages_1_Id] ([PayloadId]) VALUES (@NewPayloadId);
ELSE
	UPDATE [SignalR].[Messages_1_Id] SET [PayloadID] = @NewPayloadId;

COMMIT TRANSACTION;

-- Garbage collection
DECLARE @MaxTableSize int,
		@BlockSize int;

SET @MaxTableSize = 10000;
SET @BlockSize = 2500;

-- Check the table size on every Nth insert where N is @BlockSize
IF @NewPayloadId % @BlockSize = 0
	BEGIN
		-- SET NOCOUNT ON added to prevent extra result sets from
		-- interfering with SELECT statements
		SET NOCOUNT ON;

		DECLARE @RowCount int,
				@StartPayloadId bigint,
				@EndPayloadId bigint;

		BEGIN TRANSACTION;

		SELECT @RowCount = COUNT([PayloadId]), @StartPayloadId = MIN([PayloadId])
		FROM [SignalR].[Messages_1];

		-- Check if we're over the max table size
		IF @RowCount >= @MaxTableSize
			BEGIN
				DECLARE @OverMaxBy int;

				-- We want to delete enough rows to bring the table back to max size - block size
				SET @OverMaxBy = @RowCount - @MaxTableSize;
				SET @EndPayloadId = @StartPayloadId + @BlockSize + @OverMaxBy;
 
				-- Delete oldest block of messages
				DELETE FROM [SignalR].[Messages_1]
				WHERE [PayloadId] BETWEEN @StartPayloadId AND @EndPayloadId;
			END

		COMMIT TRANSACTION;
	END