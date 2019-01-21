using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TTF.Utils
{
    public class DailyRosterConstants
    {

        public const string SPECIAL_DUTY_REST = "REST";

        public const string SPECIAL_DUTY_OFF = "OFF";

        public const string SPECIAL_DUTY_CALL_BACK_OFF = "CO";

        public const string SPECIAL_DUTY_CALL_BACK_REST = "CR";

        public const string DUTY_SCHEDULE_TYPE_DRIVING = "Driving";

        public const string DUTY_SCHEDULE_TYPE_BDE = "BDE";

        public const string DUTY_SCHEDULE_TYPE_STEP_OVER = "StepOver";

        public const string DUTY_SCHEDULE_TYPE_MEAL_BREAK = "MealBreak";

        public const int MERGE_GAP_SECONDS = 1200;

        public const int DUTY_SCHEDULE_TYPE_ID_DRIVING = 1;

        public const string DUTY_SCHEDULE_TRAIN_PREPARATION = "TrainPrep";

        public const string TRAIN_PREPARATION_TASK_DIVIDED_LINE = "03:00:00";

        public const string TRAIN_PREP_TASK_TYPE_PLANNED = "PlannedWorkPiece";

        public const string TRAIN_PREP_TASK_TYPE_DAILY_PLANNED = "DailyPlannedWorkPiece";

        public const int MAX_MONTHLY_OT_IN_SECONDS = 72 * 60 * 60;

        public const int SPLIT_DUTY_GAP_STANDARD = 3600;

    }
}