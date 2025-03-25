using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Text.Json;
using Khronos4.Models;
using Microsoft.AspNetCore.Mvc;

namespace Khronos4.Pages
{
    public class MeetingPreviewModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public string Date { get; set; }
        [BindProperty(SupportsGet = true)] public string CongregationName { get; set; }
        [BindProperty(SupportsGet = true)] public string Chairman { get; set; }
        [BindProperty(SupportsGet = true)] public string AuxiliaryClassroomCounselor { get; set; }
        [BindProperty(SupportsGet = true)] public List<string> StartTimes { get; set; }
        [BindProperty(SupportsGet = true)] public string OpeningSong { get; set; }
        [BindProperty(SupportsGet = true)] public string OpeningPrayer { get; set; }
        [BindProperty(SupportsGet = true)] public string TreasuresTalkPart { get; set; }
        [BindProperty(SupportsGet = true)] public string TreasuresTalkSpeaker { get; set; }
        [BindProperty(SupportsGet = true)] public string SpiritualGemsPart { get; set; }
        [BindProperty(SupportsGet = true)] public string SpiritualGemsSpeaker { get; set; }
        [BindProperty(SupportsGet = true)] public string BibleReadingPart { get; set; }
        [BindProperty(SupportsGet = true)] public string BibleReadingStudent { get; set; }
        [BindProperty(SupportsGet = true)] public List<string> StudentAssignment { get; set; }
        [BindProperty(SupportsGet = true)] public List<string> StudentAssistant { get; set; }
        [BindProperty(SupportsGet = true)] public string MiddleSong { get; set; }
        [BindProperty(SupportsGet = true)] public List<string> ElderAssignment { get; set; }
        [BindProperty(SupportsGet = true)] public List<string> CbsReader { get; set; }
        [BindProperty(SupportsGet = true)] public string ClosingSong { get; set; }
        [BindProperty(SupportsGet = true)] public string ClosingPrayer { get; set; }

        public void OnGet()
        {
            if (TempData.ContainsKey("MeetingData"))
            {
                var jsonData = TempData["MeetingData"] as string;
                if (!string.IsNullOrEmpty(jsonData))
                {
                    var meetingData = JsonSerializer.Deserialize<MeetingPreviewModel>(jsonData);
                    if (meetingData != null)
                    {
                        Date = meetingData.Date;
                        CongregationName = meetingData.CongregationName;
                        Chairman = meetingData.Chairman;
                        AuxiliaryClassroomCounselor = meetingData.AuxiliaryClassroomCounselor;
                        StartTimes = meetingData.StartTimes;
                        OpeningSong = meetingData.OpeningSong;
                        OpeningPrayer = meetingData.OpeningPrayer;
                        TreasuresTalkPart = meetingData.TreasuresTalkPart;
                        TreasuresTalkSpeaker = meetingData.TreasuresTalkSpeaker;
                        SpiritualGemsPart = meetingData.SpiritualGemsPart;
                        SpiritualGemsSpeaker = meetingData.SpiritualGemsSpeaker;
                        BibleReadingPart = meetingData.BibleReadingPart;
                        BibleReadingStudent = meetingData.BibleReadingStudent;
                        StudentAssignment = meetingData.StudentAssignment;
                        StudentAssistant = meetingData.StudentAssistant;
                        MiddleSong = meetingData.MiddleSong;
                        ElderAssignment = meetingData.ElderAssignment;
                        CbsReader = meetingData.CbsReader;
                        ClosingSong = meetingData.ClosingSong;
                        ClosingPrayer = meetingData.ClosingPrayer;
                    }
                }
            }
        }
    }
}
