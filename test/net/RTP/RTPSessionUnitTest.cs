﻿//-----------------------------------------------------------------------------
// Filename: RTPSessionUnitTest.cs
//
// Description: Unit tests for the RTPSession class.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 01 May 2020	Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using SIPSorcery.Sys;
using Xunit;

namespace SIPSorcery.Net.UnitTests
{
    [Trait("Category", "unit")]
    public class RTPSessionUnitTest
    {
        private Microsoft.Extensions.Logging.ILogger logger = null;

        public RTPSessionUnitTest(Xunit.Abstractions.ITestOutputHelper output)
        {
            logger = SIPSorcery.UnitTests.TestLogHelper.InitTestLogger(output);
        }

        /// <summary>
        /// Checks that the RTP media state gets set correctly when:
        ///  - Audio only,
        ///  - Offer was generated locally,
        ///  - Remote party provided the answer.
        /// </summary>
        [Fact]
        public void AudioOnlyOfferAnswerTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Create two RTP sessions. First one acts as the local session to generate the offer.
            // Second one acts as the remote session to generate the answer.

            RTPSession localSession = new RTPSession(false, false, false);
            MediaStreamTrack localAudioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });
            localSession.addTrack(localAudioTrack);

            // Generate the offer to send to the remote party.
            var offer = localSession.CreateOffer(IPAddress.Loopback);

            logger.LogDebug("Local offer: " + offer.ToString());

            RTPSession remoteSession = new RTPSession(false, false, false);
            // The track for the track for the remote session is still local relative to the session it's being added to.
            MediaStreamTrack remoteAudioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });
            remoteSession.addTrack(remoteAudioTrack);

            var result = remoteSession.SetRemoteDescription(offer);

            logger.LogDebug($"Set remote description on remote session result {result}.");

            Assert.Equal(SetDescriptionResultEnum.OK, result);

            // Get the answer from the remote session.
            var answer = remoteSession.CreateAnswer(IPAddress.Loopback);

            logger.LogDebug("Remote answer: " + offer.ToString());

            // Provide the answer back to the local session.
            result = localSession.SetRemoteDescription(answer);

            logger.LogDebug($"Set remote description on local session result {result}.");

            Assert.Equal(SetDescriptionResultEnum.OK, result);
        }

        /// <summary>
        /// Checks that setting the remote description returns the correct error status when
        /// an attempt is made to the remote description on a session with no tracks.
        /// </summary>
        [Fact]
        public void NoLocalTracksTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            // Create two RTP sessions. First one acts as the local session to generate the offer.
            // Second one acts as the remote session to generate the answer.

            // A local session is created but NO media tracks are added to it.
            RTPSession localSession = new RTPSession(false, false, false);

            // Create a remote session WITH an audio track.
            RTPSession remoteSession = new RTPSession(false, false, false);
            // The track for the track for the remote session is still local relative to the session it's being added to.
            MediaStreamTrack remoteAudioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });
            remoteSession.addTrack(remoteAudioTrack);

            var offer = remoteSession.CreateOffer(IPAddress.Loopback);

            // Give the offer to the local session that is missing any media tracks.
            var result = localSession.SetRemoteDescription(offer);

            logger.LogDebug($"Set remote description on local session result {result}.");

            Assert.Equal(SetDescriptionResultEnum.NoLocalMedia, result);
        }

        /// <summary>
        /// Checks that the correct failure condition is returned when a remote description is provided
        /// with no media announcements.
        /// </summary>
        [Fact]
        public void NoRemoteMediaTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            RTPSession localSession = new RTPSession(false, false, false);
            MediaStreamTrack localAudioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });
            localSession.addTrack(localAudioTrack);

            var remoteOffer = new SDP();

            var result = localSession.SetRemoteDescription(remoteOffer);

            logger.LogDebug($"Set remote description on local session result {result}.");

            Assert.Equal(SetDescriptionResultEnum.NoRemoteMedia, result);
        }

        /// <summary>
        /// Checks that the correct failure condition is returned when the local session has a single
        /// media track which is a different type to a single remote media announcement.
        /// </summary>
        [Fact]
        public void NoMatchingMediaTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            RTPSession localSession = new RTPSession(false, false, false);
            MediaStreamTrack localAudioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });
            localSession.addTrack(localAudioTrack);

            RTPSession remoteSession = new RTPSession(false, false, false);
            // The track for the track for the remote session is still local relative to the session it's being added to.
            MediaStreamTrack remoteVideoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.VP8) });
            remoteSession.addTrack(remoteVideoTrack);

            var result = localSession.SetRemoteDescription(remoteSession.CreateOffer(IPAddress.Loopback));

            logger.LogDebug($"Set remote description on local session result {result}.");

            Assert.Equal(SetDescriptionResultEnum.NoMatchingMediaType, result);
        }

        /// <summary>
        /// Checks that the correct failure condition is returned when a remote description is provided
        /// with an invalid connection port.
        /// </summary>
        [Fact]
        public void InvalidPortInRemoteOfferTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            RTPSession localSession = new RTPSession(false, false, false);
            MediaStreamTrack localAudioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });
            localSession.addTrack(localAudioTrack);

            var remoteOffer = new SDP();
            remoteOffer.SessionId = Crypto.GetRandomInt(5).ToString();

            remoteOffer.Connection = new SDPConnectionInformation(IPAddress.Loopback);

            SDPMediaAnnouncement audioAnnouncement = new SDPMediaAnnouncement(
                SDPMediaTypesEnum.audio,
                66000,
                new List<SDPMediaFormat> { new SDPMediaFormat(SDPMediaFormatsEnum.PCMU) });

            audioAnnouncement.Transport = RTPSession.RTP_MEDIA_PROFILE;

            remoteOffer.Media.Add(audioAnnouncement);

            var result = localSession.SetRemoteDescription(remoteOffer);

            logger.LogDebug($"Set remote description on local session result {result}.");

            Assert.Equal(SetDescriptionResultEnum.InvalidAudioPort, result);
        }
    }
}