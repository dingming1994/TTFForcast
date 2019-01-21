-- Drop current foreign keys
ALTER TABLE [dbo].[DailyTrainStationTime] DROP CONSTRAINT [FK_DailyTrainStationTime_BoundID]
GO

-- Drop indices except primary key
ALTER TABLE [dbo].[DailyTrainStationTime] DROP CONSTRAINT [DF_DailyTrainStationTime_OpsDate]
GO

DROP INDEX [CIX_DTST_Mix] ON [dbo].[DailyTrainStationTime]
GO

DROP INDEX [CIX_DTST_Mix2] ON [dbo].[DailyTrainStationTime]
GO

-- Drop LineID, BOundID, PlatformID
alter table dailytrainstationtime drop column lineID
alter table dailytrainstationtime drop column boundid
alter table dailytrainstationtime drop column platformid
alter table dailytrainstationtime drop column type

--Add new columns 
Alter table dailytrainstationtime add line varchar(5)
Alter table dailytrainstationtime add bnd varchar(10)
Alter table dailytrainstationtime add stn varchar(5)
Alter table dailytrainstationtime add plt varchar(10)
Alter table dailytrainstationtime add type varchar(5)


/****** Object:  Index [CIX_TimePlatType]    Script Date: 2/11/2017 5:05:29 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [CIX_TimePlatType] ON [dbo].[DailyTrainStationTime]
(
	[Time] ASC,
	[plt] ASC,
	[type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

--Modify length of "key" field in SystemLookup table
alter table systemlookup alter column [key] varchar(40)


/**************Now update table DailyForecastArrivalTime ********************/
ALTER TABLE [dbo].[DailyForecastedArrivalTime] DROP CONSTRAINT [FK_DailyForecastedArrivalTime_BoundID]
GO
ALTER TABLE [dbo].[DailyForecastedArrivalTime] DROP CONSTRAINT [FK_DailyForecastedArrivalTime_StnID]
GO
DROP INDEX [CIX_DailyForecastedArrivalTime_Platform] ON [dbo].[DailyForecastedArrivalTime]
GO
DROP INDEX [CIX_DailyForecastedArrivalTime_Stn] ON [dbo].[DailyForecastedArrivalTime]
GO

-- Drop stn, bound
alter table dailyforecastedArrivalTime drop column stn
alter table dailyforecastedArrivalTime drop column bound

--Add new columns 
Alter table dailyforecastedArrivalTime add stn varchar(5)
Alter table dailyforecastedArrivalTime add bnd varchar(10)

/******Modify BoundStation table *****************/
-- Drop boundid, stationID, and versionID
alter table Boundstation drop column boundid
alter table Boundstation drop column stationID
alter table Boundstation drop column versionID

--Add new columns 
Alter table Boundstation add stn varchar(5)
Alter table Boundstation add bnd varchar(10)
Alter table Boundstation add line varchar(10)

/****** Object:  Index [CIX_BoundStation_Comp]    Script Date: 6/12/2017 12:09:43 PM ******/
CREATE NONCLUSTERED INDEX [CIX_BoundStation_Comp] ON [dbo].[BoundStation]
(
	[Position] ASC,
	[stn] ASC,
	[bnd] ASC,
	[line] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/*****Modify DailyActualStationTime  ****/
ALTER TABLE [dbo].[DailyActualStationTime] DROP CONSTRAINT [DF_DailyActualStationTime_OpsDate]
GO
alter table DailyActualStationTime drop column OpsDate



/****** Object:  Table [dbo].[PointToPointTravelTimeToday]    Script Date: 6/12/2017 12:36:57 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PointToPointTravelTimeToday](
	[FromStn] [nvarchar](5) NOT NULL,
	[ToStn] [nvarchar](5) NOT NULL,
	[Bound] [nvarchar](10) NOT NULL,
	[ArrivalTime] [datetime] NOT NULL,
	[TravelTime] [int] NOT NULL,
	[ID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_PointToPointTravelTimeToday] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


/****** Object:  Table [dbo].[PointToPointTravelTimeArchive]    Script Date: 6/12/2017 12:46:14 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PointToPointTravelTimeArchive](
	[FromStn] [nvarchar](5) NOT NULL,
	[ToStn] [nvarchar](5) NOT NULL,
	[Bound] [nvarchar](10) NOT NULL,
	[ArrivalTime] [datetime] NOT NULL,
	[OpsDate] [datetime] NOT NULL,
	[TravelTime] [int] NOT NULL,
	[ID] [int] NOT NULL,
 CONSTRAINT [PK_PointToPointTravelTimeArchive] PRIMARY KEY CLUSTERED 
(
	[OpsDate] ASC,
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO


/****** Object:  Table [dbo].[PointToPointTravelTimeLatest]    Script Date: 6/12/2017 12:53:58 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PointToPointTravelTimeLatest](
	[FromStn] [nvarchar](5) NOT NULL,
	[ToStn] [nvarchar](5) NOT NULL,
	[Bound] [nvarchar](10) NOT NULL,
	[ArrivalTime] [datetime] NOT NULL,
	[EMU] [nvarchar](10) NOT NULL,
	[TravelTime] [int] NOT NULL,
	[ID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_PointToPointTravelTimeLatest] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/*******Make changes to TrainMovement ************/
-- Drop lastStn, bound
alter table TrainMovement drop column lastStn
alter table TrainMovement drop column bound

--Add new columns 
Alter table TrainMovement add lastStn varchar(5)
Alter table TrainMovement add bound varchar(10)

Alter TABLE Trainmovement alter column trainNo varchar(10) null
/****** Object:  Index [CIX_TrainMovement_Plat]    Script Date: 28/12/2017 10:31:24 AM ******/
CREATE NONCLUSTERED INDEX [CIX_TrainMovement_Plat] ON [dbo].[TrainMovement]
(
	[Platform] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[LastAvgTravelTime]    Script Date: 8/12/2017 2:47:59 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LastAvgTravelTime](
	[FromPlat] [nvarchar](10) NOT NULL,
	[ToPlat] [nvarchar](10) NOT NULL,
	[TravelTime] [int] NOT NULL,
	[ID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_LastAvgTravelTime] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

alter table stntostntraveltimetoday alter column travelTime int

/****** Object:  Table [dbo].[DailyForecastedArrivalTimeArchive]    Script Date: 13/12/2017 3:17:26 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[DailyForecastedArrivalTimeArchive](
	[ID] [int] NOT NULL,
	[TrainNo] [varchar](10) NOT NULL,
	[EMUNo] [varchar](10) NULL,
	[Time] [datetime] NOT NULL,
	[PlannedStationTime] [bigint] NULL,
	[TrainNoAlias] [varchar](10) NULL,
	[IsService] [bit] NOT NULL,
	[Platform] [varchar](25) NULL,
	[stn] [varchar](5) NULL,
	[bnd] [varchar](10) NULL,
	[OpsDate] [datetime] NOT NULL,
 CONSTRAINT [PK_DailyForecastedArrivalTimeArchive] PRIMARY KEY CLUSTERED 
(
	[ID] ASC,
	[OpsDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING ON
GO

/****** Modify ForecastError table **************/
alter table ForecastError DROP COLUMN MaxError
alter table ForecastError DROP COLUMN MinError
alter table ForecastError DROP COLUMN AvgError
alter table ForecastError DROP COLUMN NoStn
alter table ForecastError DROP COLUMN error


/****** Object:  Table [dbo].[ForecastError]    Script Date: 13/12/2017 3:26:50 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[ForecastErrorArchive](
	[ID] [int] NOT NULL,
	[EmuNo] [varchar](10) NOT NULL,
	[TrainNo] [varchar](5) NOT NULL,
	[Stn] [varchar](5) NOT NULL,
	[Time] [datetime] NOT NULL,
	[ForecastError] [int] NULL,
	[ActualError] [int] NULL,
	[ForecastTime] [datetime] NULL,
	[PlannedTime] [datetime] NULL,
	[ActualTime] [datetime] NULL,
	[OpsDate] [datetime] NOT NULL,
 CONSTRAINT [PK_ForecastErrorArchive] PRIMARY KEY CLUSTERED 
(
	[ID] ASC,
	[OpsDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING ON
GO


/****** Object:  Table [dbo].[AvgStnToStnTravelTimeTemp]    Script Date: 14/12/2017 4:23:53 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

/***** Create table AvgStnToStnTravelTimeTemp ********/
CREATE TABLE [dbo].[AvgStnToStnTravelTimeTemp](
	[FromPlat] [varchar](5) NOT NULL,
	[ToPlat] [varchar](5) NOT NULL,
	[FromTime] [time](7) NOT NULL,
	[ToTime] [time](7) NOT NULL,
	[TravelTime] [int] NOT NULL,
	[Date] [datetime] NOT NULL
) ON [PRIMARY]

GO

SET ANSI_PADDING ON
GO

CREATE NONCLUSTERED INDEX [CIX_AvgStnToStnTravelTimeTemp_Plat] ON [dbo].[AvgStnToStnTravelTimeTemp]
(
	[FromPlat] ASC,
	[ToPlat] ASC,
	[FromTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

/****** Object:  Index [CIX_AvgStnToStnTravelTimeTemp_Date]    Script Date: 14/12/2017 4:24:55 PM ******/
CREATE NONCLUSTERED INDEX [CIX_AvgStnToStnTravelTimeTemp_Date] ON [dbo].[AvgStnToStnTravelTimeTemp]
(
	[Date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


/****** Object:  Index [CIX_AvgStntoStnTravelTime_Comp1]    Script Date: 15/12/2017 2:05:19 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [CIX_AvgStntoStnTravelTime_Comp1] ON [dbo].[AvgStnToStnTravelTime]
(
	[FromPlat] ASC,
	[ToPlat] ASC,
	[FromTime] ASC,
	[ToTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

alter table [DailyForecastedArrivalTimeArchive] drop column isservice

/****** Object:  Index [CIX_AvgStnToStnTravelTime_DateType]    Script Date: 16/12/2017 9:47:10 AM ******/
DROP INDEX [CIX_AvgStnToStnTravelTime_DateType] ON [dbo].[AvgStnToStnTravelTime]
GO

/****** Object:  Index [CIX_AvgStnToStnTravelTime_FromStn]    Script Date: 16/12/2017 10:04:07 AM ******/
DROP INDEX [CIX_AvgStnToStnTravelTime_FromStn] ON [dbo].[AvgStnToStnTravelTime]
GO

/****** Object:  Index [CIX_AvgStnToStnTravelTime_FromTime]    Script Date: 16/12/2017 10:04:23 AM ******/
DROP INDEX [CIX_AvgStnToStnTravelTime_FromTime] ON [dbo].[AvgStnToStnTravelTime]
GO

DROP INDEX [CIX_AvgStnToStnTravelTime_ToStn] ON [dbo].[AvgStnToStnTravelTime]
GO

DROP INDEX [CIX_AvgStnToStnTravelTime_ToTime] ON [dbo].[AvgStnToStnTravelTime]
GO

Alter table station alter column [type] varchar(10)
Alter table station drop column lineid
Alter table station add  LineCode varchar(5)

/****** Object:  Index [CIX_Stn_Mix]    Script Date: 24/12/2017 10:34:00 PM ******/
CREATE NONCLUSTERED INDEX [CIX_Stn_Mix] ON [dbo].[Station]
(
	[Code] ASC,
	[LineCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

ALTER TABLE TrainMovement Add PlannedSTID bigint

ALTER TABLE StnToStnTravelTime Alter COLUMN FromPlat varchar(20)
ALTER TABLE StnToStnTravelTime Alter COLUMN ToPlat varchar(20)

ALTER TABLE StnToStnTravelTimeToday Alter COLUMN FromPlat varchar(20)
ALTER TABLE StnToStnTravelTimeToday Alter COLUMN ToPlat varchar(20)

ALTER TABLE DailyTrainStationTime Alter Column plt varchar(20)
ALTER TABLE DailyForecastedArrivalTime Alter Column plt varchar(20)

ALTER TABLE AvgStntoStnTravelTime Alter COLUMN FromPlat varchar(20)
ALTER TABLE AvgStntoStnTravelTime Alter COLUMN ToPlat varchar(20)

ALTER TABLE AvgStntoStnTravelTimeTemp Alter COLUMN FromPlat varchar(20)
ALTER TABLE AvgStntoStnTravelTimeTemp Alter COLUMN ToPlat varchar(20)

ALTER TABLE LastAvgTravelTime Alter COLUMN FromPlat varchar(20)
ALTER TABLE LastAvgTravelTime Alter COLUMN ToPlat varchar(20)