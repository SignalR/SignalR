-- Params: @Payload varbinary(max)
-- Replace: [SignalR] => [schema_name], [Messages_0 => [table_prefix_index

-- We need to ensure that the payload id increment and payload insert are atomic.
-- Hence, we explicitly need to ensure that the order of operations is correct
-- such that an exclusive lock is taken on the ID table to effectively serialize
-- the insert of new messages . It is critical that once a message with PayloadID = N
-- has been committed into the message table that a message with PayloadID < N can
-- *never* be committed.

SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;

-- START: TEST DATA --
--DECLARE @Payload varbinary(max);
--SET @Payload = 0x2605260626402642;
-- END: TEST DATA --

DECLARE @NewPayloadId bigint;

-- Update last payload id, find new PayloadId and insert new message at once.
-- Now we don't need explicit transaction here.
UPDATE TOP(1) [SignalR].[Messages_0_Id] SET @NewPayloadId = [PayloadId] = [PayloadId] + 1
OUTPUT INSERTED.[PayloadId], @Payload, GETDATE()
-- Insert payload
INTO [SignalR].[Messages_0] ([PayloadId], [Payload], [InsertedOn]);

-- Garbage collection
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
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
		FROM [SignalR].[Messages_0];

		-- Check if we're over the max table size
		IF @RowCount >= @MaxTableSize
			BEGIN
				DECLARE @OverMaxBy int;

				-- We want to delete enough rows to bring the table back to max size - block size
				SET @OverMaxBy = @RowCount - @MaxTableSize;
				SET @EndPayloadId = @StartPayloadId + @BlockSize + @OverMaxBy;
 
				-- Delete oldest block of messages
				DELETE FROM [SignalR].[Messages_0]
				WHERE [PayloadId] BETWEEN @StartPayloadId AND @EndPayloadId;
			END

		COMMIT TRANSACTION;
	END