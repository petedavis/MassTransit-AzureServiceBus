﻿// Copyright 2012 Henrik Feldt, Chris Patterson, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports.AzureServiceBus.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Framework;
    using Magnum;
    using Magnum.Extensions;
    using NUnit.Framework;


    public interface CorrelatedTestMessage
        : CorrelatedBy<Guid>
    {
        string SoundsLike { get; }
    }


    class CorrelatedTestMessageImpl :
        CorrelatedTestMessage
    {
        public CorrelatedTestMessageImpl(Guid correlationId, string soundsLike)
        {
            CorrelationId = correlationId;
            SoundsLike = soundsLike;
        }

        public Guid CorrelationId { get; private set; }
        public string SoundsLike { get; private set; }
    }


    [TestFixture]
    public class When_publishing_correlated_interface
    {
        [TestFixtureSetUp]
        public void An_interface_based_message_is_published()
        {
            _receivedAnyRat = new TaskCompletionSource<CorrelatedTestMessage>();

            var details = new AccountDetails();

            _subscriberBus = ServiceBusFactory.New(sbc =>
                {
                    var uri = details.BuildUri("test_subscriber");
                    Console.WriteLine(uri);

                    sbc.ReceiveFrom(uri);
                    sbc.SetPurgeOnStartup(true);
                    sbc.Subscribe(s => s.Handler<CorrelatedTestMessage>(x => _receivedAnyRat.TrySetResult(x)).Transient());
                    sbc.UseAzureServiceBus(x =>
                        {
                            x.ConfigureNamespace(uri.Host, h =>
                                {
                                    h.SetKeyName(AccountDetails.KeyName);
                                    h.SetKey(AccountDetails.Key);
                                });
                        });
                    sbc.UseAzureServiceBusRouting();
                });

            _publisherBus = ServiceBusFactory.New(sbc =>
                {
                    var uri = details.BuildUri("test_publisher");
                    sbc.ReceiveFrom(uri);
                    sbc.SetPurgeOnStartup(true);
                    sbc.UseAzureServiceBus(x =>
                        {
                            x.ConfigureNamespace(uri.Host, h =>
                                {
                                    h.SetKeyName(AccountDetails.KeyName);
                                    h.SetKey(AccountDetails.Key);
                                });
                        });
                    sbc.UseAzureServiceBusRouting();
                });


            _correlationId = NewId.NextGuid();
            _publisherBus.Publish<CorrelatedTestMessage>(new CorrelatedTestMessageImpl(_correlationId, "meek"));
        }

        [TestFixtureTearDown]
        public void Dispose_buses()
        {
            _publisherBus.Dispose();
            _subscriberBus.Dispose();
        }

        TaskCompletionSource<CorrelatedTestMessage> _receivedAnyRat;
        Guid _correlationId;

        IServiceBus _publisherBus;
        IServiceBus _subscriberBus;

        [Test]
        public void Sound_equals()
        {
            Assert.IsTrue(_receivedAnyRat.Task.Wait(15.Seconds()));

            Assert.AreEqual("meek", _receivedAnyRat.Task.Result.SoundsLike);
        }
    }

    #region ordinary interfaces

    public interface SwedishRat
    {
        string SoundsLike { get; set; }
    }


    public interface MongolianRat
    {
        string SoundsLike { get; set; }
    }


//	[TestFixture(typeof (SwedishRat))]
    //[TestFixture(typeof (MongolianRat))]
    //[Integration]
    public class When_publishing_ordinary_interfaces<TMesg>
        where TMesg : class, SpecialSwedishRat
    {
        //Future<TMesg> _receivedAnyRat;

        //[When]
//		public void an_interface_based_message_is_published()
//		{
//			_receivedAnyRat = new Future<TMesg>();
//
//			var details = new AccountDetails();
//
//			_publisherBus = ServiceBusFactory.New(sbc =>
//				{
//					sbc.ReceiveFrom(details.BuildUri("When_publishing_ordinary_interfaces_publisher"));
//					sbc.SetPurgeOnStartup(true);
//					sbc.UseAzureServiceBus();
//					sbc.UseAzureServiceBusRouting();
//				});
//
//			_subscriberBus = ServiceBusFactory.New(sbc =>
//				{
//					sbc.ReceiveFrom(details.BuildUri("When_publishing_ordinary_interfaces_subscriber"));
//					sbc.SetPurgeOnStartup(true);
//					sbc.Subscribe(s => s.Handler<TMesg>(_receivedAnyRat.Complete).Transient());
//					sbc.UseAzureServiceBus();
//					sbc.UseAzureServiceBusRouting();
//				});
//
//			// wait for the inbound transport to become ready before publishing
//			_subscriberBus.Endpoint.InboundTransport.Receive(c1 => c2 => { }, 1.Milliseconds());
//
//			_publisherBus.Publish<TMesg>(new
//				{
//					SoundsLike = "peep"
//				});
//
//			_receivedAnyRat.WaitUntilCompleted(35.Seconds()).ShouldBeTrue();
//		}
//
//		IServiceBus _publisherBus;
//		IServiceBus _subscriberBus;
//
//		[Then]
//		public void sound_equals()
//		{
//			TMesg val = _receivedAnyRat.Value;
//			string sound = val.SoundsLike;
//			sound.ShouldBeEqualTo("peep");
//		}
//
//		[Finally]
//		public void dispose_buses()
//		{
//			_publisherBus.Dispose();
//			_subscriberBus.Dispose();
//		}
    }

    #endregion

    #region subtype interface

    public interface SpecialSwedishRat
    {
        string SoundsLike { get; }

        /// <summary>The Swedes tag all rats</summary>
        byte[] AnimalTag { get; }
    }


    public interface SvenskSkogsråtta
        : SpecialSwedishRat
    {
        bool GömmerSigIMyllan { get; }
    }


    public class DanneDenGråafläckigaRåttan : SvenskSkogsråtta,
                                              IEquatable<SvenskSkogsråtta>
    {
        public string SoundsLike { get; set; }
        public byte[] AnimalTag { get; set; }
        public bool GömmerSigIMyllan { get; set; }

        #region eq

        public bool Equals(SvenskSkogsråtta other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.AnimalTag.SequenceEqual(AnimalTag)
                   && Equals(other.SoundsLike, SoundsLike)
                   && other.GömmerSigIMyllan.Equals(GömmerSigIMyllan);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(SvenskSkogsråtta))
                return false;
            return Equals((SvenskSkogsråtta) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = AnimalTag.GetHashCode();
                result = (result * 397) ^ SoundsLike.GetHashCode();
                result = (result * 397) ^ GömmerSigIMyllan.GetHashCode();
                return result;
            }
        }

        #endregion
    }


    public class LisaDenBrunspräckligaRåttan : SvenskSkogsråtta,
                                               IEquatable<SvenskSkogsråtta>
    {
        public string SoundsLike { get; private set; }
        public bool GömmerSigIMyllan { get; set; }
        public byte[] AnimalTag { get; private set; }

        #region eq

        public bool Equals(SvenskSkogsråtta other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.AnimalTag.SequenceEqual(AnimalTag)
                   && Equals(other.SoundsLike, SoundsLike)
                   && other.GömmerSigIMyllan.Equals(GömmerSigIMyllan);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(SvenskSkogsråtta))
                return false;
            return Equals((SvenskSkogsråtta) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = AnimalTag.GetHashCode();
                result = (result * 397) ^ SoundsLike.GetHashCode();
                result = (result * 397) ^ GömmerSigIMyllan.GetHashCode();
                return result;
            }
        }

        #endregion
    }


//
//	[Integration, Category("PolymorphicRouting"), Ignore("Next up for implementation!")]
//	public class When_publishing_subtype_interfaces_single_publish_and_subscribing_supertype
//		: EndpointTestFixture<AzureServiceBusTransportFactory>
//	{
//		ConsumerOf<SpecialSwedishRat> supertypeConsumer;
//		ConsumerOf<SvenskSkogsråtta> subtypeConsumer;
//			
//		[When]
//		public void an_interface_based_message_is_published()
//		{
//			supertypeConsumer = new ConsumerOf<SpecialSwedishRat>();
//			subtypeConsumer = new ConsumerOf<SvenskSkogsråtta>();
//
//			var details = new AccountDetails();
//
//			PublisherBus = SetupServiceBus(details.BuildUri("When_publishing_subsuper_publisher"), cfg =>
//				{
//					cfg.SetPurgeOnStartup(true);
//					cfg.UseAzureServiceBusRouting();
//				});
//			SubscriberBus = SetupServiceBus(details.BuildUri("When_publishing_subtype_interfaces_single_publish_and_subscribing_supertype_subscriber"), cfg =>
//				{
//					cfg.SetPurgeOnStartup(true);
//					cfg.Subscribe(s =>
//						{
//							s.Instance(supertypeConsumer).Transient();
//							s.Instance(subtypeConsumer).Transient();
//						});
//					cfg.UseAzureServiceBusRouting();
//				});
//
//			// wait for the inbound transport to become ready before publishing
//			SubscriberBus.Endpoint.InboundTransport.Receive(c1 => c2 => { }, 1.Milliseconds());
//
//			PublisherBus.Publish<SpecialSwedishRat>(new DanneDenGråafläckigaRåttan
//				{
//					SoundsLike = "meeeäää",
//					GömmerSigIMyllan = true,
//					AnimalTag = new byte[] { 1, 2, 3 }
//				});
//		}
//
//		protected IServiceBus PublisherBus { get; private set; }
//		protected IServiceBus SubscriberBus { get; private set; }
//
//		[Test]
//		public void supertype_consumer_should_receive_correct_message()
//		{
//			supertypeConsumer.ShouldHaveReceived(new DanneDenGråafläckigaRåttan
//				{
//					SoundsLike = "meeeäää",
//					GömmerSigIMyllan = true,
//					AnimalTag = new byte[] { 1,2,3 }
//				});
//		}
//
//		[Test]
//		public void subtype_consumer_should_receive_correct_message()
//		{
//			subtypeConsumer.ShouldHaveReceived(new DanneDenGråafläckigaRåttan
//				{
//					SoundsLike = "meeeäää",
//					GömmerSigIMyllan = true,
//					AnimalTag = new byte[] { 1, 2, 3 }
//				});
//		}
//
//		[Test]
//		public void supertype_received_only_one_message()
//		{
//			supertypeConsumer.ReceivedMessageCount
//				.ShouldBeEqualTo(1);
//		}
//
//		[Test]
//		public void subtype_received_only_one_message()
//		{
//			subtypeConsumer.ReceivedMessageCount
//				.ShouldBeEqualTo(1);
//		}
//	}

    #endregion
}