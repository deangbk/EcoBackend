using System;
using System.Collections.Generic;

using Survey_Backend.Models;
using Survey_Backend.Controllers;

namespace Survey_Backend.DTO {
	/// <summary>
	/// For <see cref="SurveyController.CreateSurveySet"/>
	/// </summary>
	public class SurveyCreate {
		public string ProjectName { get; set; }

		public string SurveyDescription { get; set; }
	}

	/// <summary>
	/// For <see cref="QuestionController.GetResponse"/>
	/// </summary>
	public class GetResponse {
		public int questionId;
		public Guid responderId;
	}


}
